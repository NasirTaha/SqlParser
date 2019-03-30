using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQL_Parser.Extensions;

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
                "<", ">", "<=", ">=", "<>", "=", "IS"
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
                    int counter = 0;
                    foreach (var statement in statements)
                    {
                        string firstWord = statement.Substring(0, statement.IndexOf(' ')).Trim().ToUpper();
                        switch (firstWord)
                        {
                            case "USE":
                                AddProperty(statExpandoObject, $"Statement{counter++}", ParseUse(statement));
                                break;
                            case "SELECT":
                                AddProperty(statExpandoObject, $"Statement{counter++}", ParseSelect(statement));
                                break;
                            case "INSERT":
                                AddProperty(statExpandoObject, $"Statement{counter++}", ParseInsert(statement));
                                break;
                            case "DELETE":
                                AddProperty(statExpandoObject, $"Statement{counter++}", ParseDelete(statement));

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


                return new JsonResult(new { statements = statExpandoObject });


            }
            catch (Exception ex)
            {
                //resultString.Add("Exception occured");
                //resultString.Add(ex.Message);
                return new JsonResult(new { SyntaxError = ex.Message });
            }
        }

        private dynamic ParseUse(string statement)
        {
            dynamic use = new ExpandoObject();
            use.Type = "USE";
            use.Database_Name = SubstringExtensions.Between(statement, "USE", ";", true);
            return use;
        }

        private dynamic ParseDelete(string statement)
        {
            dynamic delete = new ExpandoObject();
            delete.Type = "DELETE";
            if (!statement.Contains("WHERE"))
            {
                var tableName = SubstringExtensions.Between(statement, "DELETE FROM", ";", true);
                delete.Table = tableName;
            }
            else
            {
                var tableName = SubstringExtensions.Between(statement, "DELETE FROM", "WHERE", true);
                delete.Table = tableName;

                dynamic wherExpandoObject = new ExpandoObject();
                var whereText = SubstringExtensions.Between(statement, "WHERE", ";", true);
                int counter = 0;
                foreach (var oper in ComparisonOperators)
                {
                    if (whereText.Contains(oper))
                    {
                        dynamic critera = new ExpandoObject();
                        critera.Left = SubstringExtensions.Before(whereText, oper);
                        critera.Operator = oper;
                        critera.Right = SubstringExtensions.After(whereText, oper);
                        AddProperty(wherExpandoObject, counter++.ToString(), critera);
                    }
                }
                AddProperty(delete, "Where", wherExpandoObject);


            }

            return delete;
        }

        private dynamic ParseInsert(string statement)
        {
            dynamic insert = new ExpandoObject();
            insert.Type = "INSERT";
            var insertText = SubstringExtensions.Between(statement, "INSERT INTO", "(", true);
            insert.Table = insertText.Trim();
            var columnstText = SubstringExtensions.Between(statement, "(", ")", true);
            var columnsArray = columnstText.Split(',');

            var valuesText = SubstringExtensions.Between(statement, "VALUES (", ");", true);
            var valuesArray = valuesText.Split(',');
            dynamic columns = new ExpandoObject();

            for (int i = 0; i < columnsArray.Length; i++)
            {
                dynamic column = new ExpandoObject();
                column.Name = columnsArray[i];
                column.Value = valuesArray[i];
                AddProperty(columns, i.ToString(), column);
            }

            AddProperty(insert, "Columns", columns);

            return insert;
        }

        private dynamic ParseSelect(string statement)
        {
            dynamic select = new ExpandoObject();
            select.Type = "SELECT";
            dynamic columns = new ExpandoObject();
            //Split statement to words
            var words = SubstringExtensions.Between(statement, "SELECT", "FROM").Trim().Replace(" ", "");
            var columnNames = words.Split(',');

            //Column names
            foreach (var colName in columnNames)
            {
                dynamic col1 = new ExpandoObject();
                col1.Type = "Column";
                col1.Name = colName.Replace(",", "").ToLower();
                AddProperty(columns, columnNames.IndexOf(colName).ToString(), col1);
            }
            AddProperty(select, "Columns", columns);

            //Table name and aliases
            dynamic table = new ExpandoObject();
            var tableNameAndAliases = SubstringExtensions.Between(statement, "FROM", "WHERE").Trim();
            if (tableNameAndAliases.Contains("AS"))
            {
                var tableName = SubstringExtensions.Between(statement, "FROM", "AS").Trim();
                var alises = SubstringExtensions.Between(statement, "AS", "WHERE").Trim();

                table.Type = "Table";
                table.Name = tableName.ToLower();
                table.Alises = alises.ToLower();
            }
            else
            {
                var tableName = SubstringExtensions.Between(statement, "FROM", "WHERE").Trim();
                table.Type = "Table";
                table.Name = tableName.ToLower();
            }
            AddProperty(select, "From", table);

            //Criteria without order by
            if (statement.Contains("WHERE") && !statement.Contains("ORDER BY"))
            {
                dynamic where = new ExpandoObject();
                var wherePhrase = SubstringExtensions.Between(statement, "WHERE", ";").Trim();
                var whereArray = wherePhrase.Split(' ');
                int counter = 0;
                foreach (var oper in ComparisonOperators)
                {
                    for (int i = 0; i < whereArray.Length; i++)
                    {
                        if (oper == whereArray[i])
                        {
                            dynamic crit = new ExpandoObject();
                            crit.Left = whereArray[i - 1];
                            crit.Operator = oper;
                            crit.Right = SubstringExtensions.After(wherePhrase, oper).Trim();

                            AddProperty(where, counter++.ToString(), crit);
                        }
                    }
                }
                if (tableNameAndAliases.Contains("AS"))
                {
                    var tableName = SubstringExtensions.Between(statement, "FROM", "AS").Trim();
                    var alises = SubstringExtensions.Between(statement, "AS", "WHERE").Trim();

                    table.Type = "Table";
                    table.Name = tableName.ToLower();
                    table.Alises = alises.ToLower();
                }
                else
                {
                    var tableName = SubstringExtensions.Between(statement, "FROM", "WHERE").Trim();
                    table.Type = "Table";
                    table.Name = tableName.ToLower();
                }

                AddProperty(select, "Where", where);
            }
            else
                if (statement.Contains("WHERE") && statement.Contains("ORDER BY"))
            {
                dynamic where = new ExpandoObject();
                var wherePhrase = SubstringExtensions.Between(statement, "WHERE", "ORDER BY").Trim();
                var whereArray = wherePhrase.Split(' ');
                int counter = 0;
                foreach (var oper in ComparisonOperators)
                {
                    for (int i = 0; i < whereArray.Length; i++)
                    {
                        if (oper == whereArray[i])
                        {
                            dynamic crit = new ExpandoObject();
                            crit.Left = whereArray[i - 1];
                            crit.Operator = oper;
                            crit.Right = SubstringExtensions.After(wherePhrase, oper).Trim();

                            AddProperty(where, counter++.ToString(), crit);
                        }
                    }
                }
                if (tableNameAndAliases.Contains("AS"))
                {
                    var tableName = SubstringExtensions.Between(statement, "FROM", "AS").Trim();
                    var alises = SubstringExtensions.Between(statement, "AS", "WHERE").Trim();

                    table.Type = "Table";
                    table.Name = tableName.ToLower();
                    table.Alises = alises.ToLower();
                }
                else
                {
                    var tableName = SubstringExtensions.Between(statement, "FROM", "WHERE").Trim();
                    table.Type = "Table";
                    table.Name = tableName.ToLower();
                }

                AddProperty(select, "Where", where);

                //Order by
                var orderPhrase = SubstringExtensions.Between(statement, "ORDER BY", ";").Trim();
                dynamic orderBy = new ExpandoObject();
                orderBy.Type = "Order by";
                orderBy.column = orderPhrase;

                AddProperty(select, "Where", orderBy);

            }


            return select;
        }

        private List<string> GetStatements(string queryText)
        {
            List<string> statements = new List<string>();

            queryText = queryText.Trim();
            string remaining = queryText;
            do
            {
                var endStatementPosition = remaining.IndexOf(';');
                var statement = remaining.Substring(0, endStatementPosition + 1);
                remaining = remaining.Substring(endStatementPosition + 1, remaining.Length - statement.Length);
                if (!string.IsNullOrEmpty(statement))
                {
                    statements.Add(statement.Trim().ToUpper());
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
