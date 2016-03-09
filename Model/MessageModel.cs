using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class MessageModel
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public DocumentType DocType { get; set; }

        public override string ToString()
        {
            return Title;   //Title + "_" + Author + "_" + Enum.GetName(DocType.GetType(), DocType);
        }

        /// <summary>
        /// 验证消息，Title与Author不能为空
        /// </summary>
        /// <returns></returns>
        public bool IsVlid()
        {
            return !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Author);
        }
    }
}
