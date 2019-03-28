using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace SQL_Parser.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string Result { get; set; }
        public void OnGet()
        {
        }

        public JsonResult OnPostParse()
        {
            List<string> resultString = new List<string>();
            try
            {
                string queryText = "";
                MemoryStream stream = new MemoryStream();
                Request.Body.CopyTo(stream);
                stream.Position = 0;
                using (StreamReader reader = new StreamReader(stream))
                {
                    string requestBody = reader.ReadToEnd();
                    if (requestBody.Length > 0)
                    {
                        var obj = JsonConvert.DeserializeObject<Query>(requestBody);
                        if (obj != null)
                        {
                            queryText = obj.QueryText;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(queryText))
                {
                    resultString = GetStatements(queryText);
                }

            }
            catch (Exception ex)
            {
                resultString.Add(ex.Message);
            }

            return new JsonResult(resultString);
        }

        private List<string> GetStatements(string queryText)
        {
            List<string> statements = new List<string>();
            queryText = queryText.Trim();
            string remaining = queryText;
            do
            {
                var endStatementPosition = remaining.IndexOf(';');
                var statement = remaining.Substring(0, endStatementPosition+1);
                remaining = remaining.Substring(endStatementPosition+ 1, remaining.Length - statement.Length);
                if (!string.IsNullOrEmpty(statement))
                {
                    statements.Add(statement);
                }
            } while (remaining.Length > 0); 
            
           

            return statements;
        }
    }
    public class Query
    {
        public string QueryText { get; set; }
    }
}
