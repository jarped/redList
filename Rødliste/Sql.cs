﻿using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Npgsql;

namespace Rødliste
{
    internal class Sql
    {
        public List<string> From { get; set; }
        public List<string> Where { get; set; }

        public static void Execute(Regel regel, string configFile)
        {
            var connString = CreateConnectionstring(configFile);

            var sql = CreateSqlStringForRegel(regel.Sql);
            var trimChars = new [] {'{', '}'};

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if(reader.HasRows) regel.Naturområder = new List<string>();
                    while (reader.Read())
                    {
                        var localid = reader.GetString(0);
                        localid = localid.Trim(trimChars);
                        regel.Naturområder.Add(localid);
                    }
                }
            }
        }

        private static string CreateConnectionstring(string configFile)
        {
            dynamic config = JsonConvert.DeserializeObject(File.ReadAllText(configFile));
            return $"Host={config.host};Username={config.user};Password={config.pass};Database={config.db}";
        }

        private static string CreateSqlStringForRegel(Sql regelSql)
        {
            var sql = "SELECT l_g.localid FROM data.localid_geometry l_g, " + string.Join(",", regelSql.From);

            sql += " WHERE " + string.Join(" AND ", regelSql.Where) + " AND l_g.geometry_id = na.geometry_id";

            return sql;
        }
    }
}