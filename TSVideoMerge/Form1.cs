using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSVideoMerge
{
    public partial class Form1 : Form
    {
        // 取消令牌源，用于安全终止所有合并任务
        private CancellationTokenSource _cts;
        // 最大递归深度常量
        private const int MAX_DEPTH = 6;

        public Form1()
        {
            InitializeComponent();
            // 初始化DataGridView列（建议在设计器中配置，这里做兜底）
            InitializeDataGridView();
            // 初始化取消令牌
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// 初始化DataGridView列配置
        /// </summary>
        private void InitializeDataGridView()
        {
            if (dataGridView1.Columns.Count == 0)
            {
                dataGridView1.Columns.AddRange(new[]
                {
                    new DataGridViewTextBoxColumn { Name = "colPath", HeaderText = "TS文件目录", Width = 300 },
                    new DataGridViewTextBoxColumn { Name = "colFileName", HeaderText = "输出文件名", Width = 150 },
                    new DataGridViewTextBoxColumn { Name = "colFileCount", HeaderText = "TS文件数", Width = 80 },
                    new DataGridViewTextBoxColumn { Name = "colProgress", HeaderText = "进度(%)", Width = 80 }
                });
                dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            }
        }

        /// <summary>
        /// 选择TS文件目录
        /// </summary>
        private void textBox1_Click(object sender, EventArgs e)
        {
            // 使用using确保资源释放，避免重复使用同一实例导致的问题
            using (var folderDialog = new FolderBrowserDialog { Description = "选择包含TS文件的根目录" })
            {
                if (folderDialog.ShowDialog(this) == DialogResult.OK)
                {
                    textBox1.Text = folderDialog.SelectedPath;
                }
            }
        }

        /// <summary>
        /// 扫描TS文件目录
        /// </summary>
        private async void button1_Click(object sender, EventArgs e)
        {
            // 禁用按钮防止重复点击
            button1.Enabled = false;
            try
            {
                var selectedPath = textBox1.Text.Trim();
                if (string.IsNullOrEmpty(selectedPath))
                {
                    MessageBox.Show("请选择TS文件目录！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!Directory.Exists(selectedPath))
                {
                    MessageBox.Show("所选目录不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 清空表格
                dataGridView1.Rows.Clear();
                // 异步扫描目录，避免UI卡顿
                var directories = await Task.Run(() => FindDirectoriesWithTsFiles(selectedPath));

                // 批量添加行，提升性能
                var rows = new List<object[]>();
                foreach (var dir in directories)
                {
                    var fileCount = Directory.GetFiles(dir, "*.ts", SearchOption.TopDirectoryOnly).Length;
                    var dirParts = dir.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                    var fileName = dirParts.Last().ToLower() == "video"
                        ? dirParts[dirParts.Length - 2]
                        : dirParts.Last();
                    rows.Add(new object[] { dir, fileName, fileCount, 0 });
                }

                // 一次性添加所有行，减少UI刷新次数
                foreach (var row in rows)
                {
                    dataGridView1.Rows.Add(row);
                }

                MessageBox.Show($"扫描完成！共找到 {directories.Count} 个包含TS文件的目录", "完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"扫描目录出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 恢复按钮状态
                button1.Enabled = true;
            }
        }

        /// <summary>
        /// 开始合并TS文件为MP4
        /// </summary>
        private async void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("请先扫描TS文件目录！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 重置取消令牌
            if (_cts.IsCancellationRequested)
            {
                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }

            // 禁用按钮防止重复操作
            button2.Enabled = false;
            try
            {
                // 收集所有待合并的任务
                var mergeTasks = new List<Task>();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue; // 跳过新行

                    var dirPath = row.Cells["colPath"].Value?.ToString();
                    var outputFileName = row.Cells["colFileName"].Value?.ToString();
                    if (string.IsNullOrEmpty(dirPath) || string.IsNullOrEmpty(outputFileName))
                    {
                        continue;
                    }

                    // 启动合并任务，传入取消令牌
                    var task = MergeTsFilesAsync(row, dirPath, outputFileName, _cts.Token);
                    mergeTasks.Add(task);
                }

                // 等待所有任务完成
                await Task.WhenAll(mergeTasks);
                MessageBox.Show("所有视频合并完成！", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("合并操作已取消！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"合并出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 恢复按钮状态
                button2.Enabled = true;
            }
        }

        /// <summary>
        /// 异步合并TS文件为MP4
        /// </summary>
        /// <param name="row">DataGridView行对象</param>
        /// <param name="dirPath">TS文件目录</param>
        /// <param name="outputFileName">输出文件名</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task MergeTsFilesAsync(DataGridViewRow row, string dirPath, string outputFileName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // 清理特殊字符，避免文件名错误
                var safeFileName = Regex.Replace(outputFileName, @"[<>:""/\\|?*]", "_").Replace(".m3u8", "").Replace(" ", "");
                var mp4Path = Path.Combine(dirPath, $"{safeFileName}.mp4");
                var tsListPath = Path.Combine(dirPath, "ts_list.txt");

                // 先删除旧文件
                if (File.Exists(mp4Path)) File.Delete(mp4Path);
                if (File.Exists(tsListPath)) File.Delete(tsListPath);

                // 获取目录总大小（仅TS文件）
                var dirSizeKb = await Task.Run(() => GetDirectorySize(dirPath) / 1024, cancellationToken);
                if (dirSizeKb <= 0)
                {
                    UpdateProgress(row, "无效目录", 0);
                    return;
                }

                // 生成TS文件列表（按数字排序）
                await Task.Run(() =>
                {
                    var tsFiles = Directory.GetFiles(dirPath, "*.ts", SearchOption.TopDirectoryOnly)
                        .OrderBy(file =>
                        {
                            // 提取文件名中的数字进行排序
                            var fileName = Path.GetFileNameWithoutExtension(file);
                            if (int.TryParse(fileName, out var num)) return num;
                            return 0;
                        })
                        .Select(file => $"file '{file}'")
                        .ToList();

                    File.WriteAllLines(tsListPath, tsFiles);
                }, cancellationToken);

                // 配置FFmpeg进程
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg.exe", // 建议配置FFmpeg路径或加入环境变量
                        Arguments = $"-f concat -safe 0 -i \"{tsListPath}\" -c copy \"{mp4Path}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        WorkingDirectory = dirPath
                    };

                    // 启动进程
                    process.Start();

                    // 异步读取FFmpeg输出，更新进度
                    var progressTask = Task.Run(async () =>
                    {
                        while (!process.StandardError.EndOfStream && !cancellationToken.IsCancellationRequested)
                        {
                            var line = await process.StandardError.ReadLineAsync();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            // 检测完成标志
                            if (line.StartsWith("[out#0"))
                            {
                                UpdateProgress(row, 100);
                                break;
                            }

                            // 提取进度数值
                            var sizeValue = ExtractSizeNumber(line);
                            if (sizeValue.HasValue && dirSizeKb > 0)
                            {
                                var progress = (int)Math.Min(Math.Round((sizeValue.Value / dirSizeKb) * 100), 100);
                                UpdateProgress(row, progress);
                            }
                        }
                    }, cancellationToken);

                    // 等待进程退出或取消
                    await Task.WhenAny(
                        Task.Run(() => process.WaitForExit(), cancellationToken),
                        Task.Delay(-1, cancellationToken)
                    );

                    // 确保进度任务完成
                    await progressTask;

                    // 检查取消状态
                    cancellationToken.ThrowIfCancellationRequested();

                    // 检查进程退出码
                    if (process.ExitCode != 0)
                    {
                        UpdateProgress(row, "合并失败", 0);
                        var errorOutput = await process.StandardOutput.ReadToEndAsync();
                        Debug.WriteLine($"FFmpeg错误：{errorOutput}");
                    }
                }

                // 删除临时文件
                if (File.Exists(tsListPath)) File.Delete(tsListPath);
            }
            catch (OperationCanceledException)
            {
                UpdateProgress(row, "已取消", 0);
                throw; // 向上抛出取消异常
            }
            catch (Exception ex)
            {
                UpdateProgress(row, $"错误：{ex.Message}", 0);
                Debug.WriteLine($"合并失败：{ex}");
            }
        }

        /// <summary>
        /// 安全更新DataGridView进度（跨线程安全）
        /// </summary>
        private void UpdateProgress(DataGridViewRow row, int progress)
        {
            UpdateProgress(row, null, progress);
        }

        /// <summary>
        /// 安全更新DataGridView进度（支持文本提示）
        /// </summary>
        private void UpdateProgress(DataGridViewRow row, string text, int progress)
        {
            if (dataGridView1.IsDisposed) return;

            // 跨线程更新UI
            if (dataGridView1.InvokeRequired)
            {
                dataGridView1.BeginInvoke(new Action(() =>
                {
                    if (text != null)
                    {
                        row.Cells["colProgress"].Value = text;
                    }
                    else
                    {
                        row.Cells["colProgress"].Value = progress;
                    }
                }));
            }
            else
            {
                if (text != null)
                {
                    row.Cells["colProgress"].Value = text;
                }
                else
                {
                    row.Cells["colProgress"].Value = progress;
                }
            }
        }

        /// <summary>
        /// 递归查找包含TS文件的目录（优化版）
        /// </summary>
        private HashSet<string> FindDirectoriesWithTsFiles(string directoryPath, int currentDepth = 0)
        {
            var result = new HashSet<string>();

            // 递归深度限制
            if (currentDepth > MAX_DEPTH || !Directory.Exists(directoryPath))
            {
                return result;
            }

            try
            {
                // 检查当前目录是否有TS文件
                if (Directory.GetFiles(directoryPath, "*.ts", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    result.Add(directoryPath);
                }

                // 递归处理子目录（跳过无权限的目录）
                foreach (var subDir in Directory.GetDirectories(directoryPath))
                {
                    try
                    {
                        var subDirResult = FindDirectoriesWithTsFiles(subDir, currentDepth + 1);
                        foreach (var dir in subDirResult)
                        {
                            result.Add(dir);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Debug.WriteLine($"无权限访问：{subDir}");
                        continue;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine($"无权限访问：{directoryPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"扫描目录出错：{directoryPath} - {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 计算目录中所有TS文件的总大小
        /// </summary>
        private long GetDirectorySize(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return 0;

            long totalSize = 0;
            try
            {
                // 只计算TS文件
                foreach (var file in Directory.GetFiles(directoryPath, "*.ts", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        totalSize += new FileInfo(file).Length;
                    }
                    catch (FileNotFoundException)
                    {
                        continue;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine($"无权限读取目录大小：{directoryPath}");
            }

            return totalSize;
        }

        /// <summary>
        /// 从FFmpeg输出中提取大小数值
        /// </summary>
        private static double? ExtractSizeNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // 优化正则表达式，匹配size= 后面的数字（支持KiB/MiB等单位）
            var match = Regex.Match(text, @"size=\s*(\d+(?:\.\d+)?)[kK]?i?[bB]?");
            if (match.Success && double.TryParse(match.Groups[1].Value, out var value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// 窗体关闭时释放资源
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 取消所有任务
            _cts.Cancel();
            _cts.Dispose();
            base.OnFormClosing(e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeDataGridView();
        }
    }
}