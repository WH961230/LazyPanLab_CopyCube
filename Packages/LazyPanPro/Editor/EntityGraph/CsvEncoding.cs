using System.IO;
using System.Text;

namespace LazyPan {
    /// <summary>
    /// CSV 编码统一入口 读自动识别 BOM 写带 BOM Excel 双击打开中文不乱码
    /// </summary>
    public static class CsvEncoding {
        public static readonly Encoding Utf8WithBom = new UTF8Encoding(true);

        /// <summary>按 UTF-8(含 BOM 自动识别)读全部行 兼容历史无 BOM 文件</summary>
        public static string[] ReadAllLines(string path) {
            return File.ReadAllLines(path, Encoding.UTF8);
        }

        /// <summary>按 UTF-8 带 BOM 写全部行 Excel 直接双击打开不乱码</summary>
        public static void WriteAllLines(string path, string[] lines) {
            File.WriteAllLines(path, lines, Utf8WithBom);
        }

        /// <summary>按 UTF-8(含 BOM 自动识别)读全部文本</summary>
        public static string ReadAllText(string path) {
            return File.ReadAllText(path, Encoding.UTF8);
        }

        /// <summary>按 UTF-8 带 BOM 写全部文本</summary>
        public static void WriteAllText(string path, string content) {
            File.WriteAllText(path, content, Utf8WithBom);
        }
    }
}
