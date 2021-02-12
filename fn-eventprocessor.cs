using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace fn_logicappeventlogger
{
    public static class fn_eventprocessor
    {
        [FunctionName("fn-EventLogger")]
        public static void Run([EventHubTrigger("la-runtime-eventhub", // Change event hub name
            Connection = "EventHubConnectionString")]string myEventHubMessage, ILogger log)
        {
            SqlConnection conn = null;
            log.LogInformation($"1. C# Event Hub trigger for receiving LogicApp diagnostic logs started");
            try
            {
                string dbConnStr, sp_LogEvent, sp_LogError,sp_LogProperty;

                if (String.IsNullOrEmpty(dbConnStr = Environment.GetEnvironmentVariable("LogDbConnectionString")))
                    throw new Exception("Log database connection string not configured in app settings.");
                if (String.IsNullOrEmpty(sp_LogEvent = Environment.GetEnvironmentVariable("Sp_LogRunEvent")))
                    throw new Exception("LogEvent sp not configured.");
                if (String.IsNullOrEmpty(sp_LogProperty = Environment.GetEnvironmentVariable("Sp_LogRunProperty")))
                    throw new Exception("LogProperty sp not configured.");
                if (String.IsNullOrEmpty(sp_LogError = Environment.GetEnvironmentVariable("Sp_LogRunError")))
                    throw new Exception("LogError sp not configured");

                log.LogInformation($"1.1 Configuration read successfully");

                LogicAppEvents events = JsonConvert.DeserializeObject<LogicAppEvents>(myEventHubMessage);
                log.LogInformation($"2. LogicApp diagnostic event deserialized successfully");

                using (conn = new SqlConnection(dbConnStr))
                {
                    conn.Open();
                    foreach (var evnt in events.records)
                    {
                        if (null != evnt.properties.endTime)
                        {
                            evnt.Save(conn, sp_LogEvent);
                            if (null != evnt.properties.error
                                && evnt.properties.error.code != "ActionConditionFailed")
                            {
                                evnt.properties.error.Save(evnt.properties.resource.runId
                                    , conn, sp_LogError);
                            }
                            if (null != evnt.properties.trackedProperties)
                            {
                                evnt.properties.trackedProperties.Save(evnt.properties.resource.runId
                                      , conn, sp_LogProperty);
                            }

                            log.LogInformation($"3. LogicApp diagnostic event logged in database");
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                log.LogError($"5.1 Exception message: {ex.Message} " +
                    $"for Event MessageBody: {myEventHubMessage}");
                if (ex.InnerException != null)
                {
                    log.LogError($"5.2 Inner exception: {ex.InnerException.Message}");
                }
            }
        }
    }
}
