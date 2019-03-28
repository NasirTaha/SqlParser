using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SQL_Parser.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string Result { get; set; }

        public List<String> ReservedWords { get; set; }
        public List<String> ComparisonOperators { get; set; }
        public List<String> LogicalOperators { get; set; }

        public IndexModel()
        {
            ReservedWords = new List<string>
            {
                "USE", "SELECT", "INSERT", "INTO", "DELETE",
                "FROM", "WHERE", "IS", "NOT", "NULL",
                "ORDER", "BY", "VALUES"
            };
            ComparisonOperators = new List<string>
            {
                "<", ">", "<=", ">=", "<>", "=", 
            };
        }
        public void OnGet()
        {
        }
        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }
        public JsonResult OnPostParse()
        {
            List<string> statements = new List<string>();
            dynamic statExpandoObject = new ExpandoObject();

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
                  statements = GetStatements(queryText);
                  foreach (var statement in statements)
                  {
                      string firstWord = statement.Substring(0, statement.IndexOf(' ')).Trim().ToUpper();
                      switch (firstWord)
                      {
                            case "USE":
                                AddProperty(statExpandoObject, "USE", statement);
                                break;
                          case "SELECT":
                                AddProperty(statExpandoObject, "SELECT", statement);
                                break;
                            case "INSERT":
                                AddProperty(statExpandoObject, "INSERT", statement);
                                break;
                            case "DELETE":
                                AddProperty(statExpandoObject, "DELETE", statement);

                                break;
                          
                      }

                      // statExpandoObject.Country = "USA";
                      
                  }
              }

              //dynamic childExpandoObject = new ExpandoObject();
              //  childExpandoObject.Name = "ss";
              //  childExpandoObject.Country = "UssSA";
              //  AddProperty(statExpandoObject, "Parent", childExpandoObject);
              //  dynamic statementList = new System.Dynamic.ExpandoObject();
              //  statementList.Sub = new List<dynamic>();

               
                return new JsonResult(new {statements = statExpandoObject });

                
            }
            catch (Exception ex)
            {
                //resultString.Add("Exception occured");
                //resultString.Add(ex.Message);
                return new JsonResult(ex.Message);
            }
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
                    statements.Add(statement.Trim());
                }
            } while (remaining.Length > 0); 
            
           

            return statements;
        }
    }
    public class Query
    {
        public string QueryText { get; set; }
    }

    public class ATS
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }
}
