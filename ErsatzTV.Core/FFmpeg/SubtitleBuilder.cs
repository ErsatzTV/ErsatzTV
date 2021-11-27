using System.IO;
using System.Text;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.FFmpeg
{
    public class SubtitleBuilder
    {
        private readonly ITempFilePool _tempFilePool;

        public SubtitleBuilder(ITempFilePool tempFilePool)
        {
            _tempFilePool = tempFilePool;
        }

        private string _content;
        
        public SubtitleBuilder WithContent(string content)
        {
            _content = content;
            return this;
        }

        public async Task<string> BuildFile()
        {
            string fileName = _tempFilePool.GetNextTempFile(TempFileCategory.Subtitle);

            var sb = new StringBuilder();
            sb.AppendLine("1");
            sb.AppendLine("00:00:00,000 --> 99:99:99,999");

            if (!string.IsNullOrWhiteSpace(_content))
            {
                sb.AppendLine(_content);
            }

            await File.WriteAllTextAsync(fileName, sb.ToString());

            return fileName;
        }
    }
}
