using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

namespace fn_logicappeventlogger
{

    public class LogicAppEvents
    {
        public Record[] records { get; set; }
    }
    public class Record
    {
        public DateTime? time { get; set; }
        public string workflowId { get; set; }
        public string resourceId { get; set; }
        public string category { get; set; }
        public string level { get; set; }
        public string operationName { get; set; }
        public Properties properties { get; set; }

        public void Save(SqlConnection sqlconn, string spName)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(spName, sqlconn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@logicAppName", properties.resource.workflowName));
                    cmd.Parameters.Add(new SqlParameter("@operationName", operationName));
                    cmd.Parameters.Add(new SqlParameter("@status", properties.status));
                    cmd.Parameters.Add(new SqlParameter("@resourceGroup", properties.resource.resourceGroupName));
                    cmd.Parameters.Add(new SqlParameter("@runId", properties.resource.runId));
                    cmd.Parameters.Add(new SqlParameter("@trackingId", properties.correlation.clientTrackingId));
                    cmd.Parameters.Add(new SqlParameter("@category", category));
                    cmd.Parameters.Add(new SqlParameter("@level", level));
                    cmd.Parameters.Add(new SqlParameter("@startTime", properties.startTime));
                    cmd.Parameters.Add(new SqlParameter("@endTime", properties.endTime));

                    cmd.Parameters.Add(new SqlParameter("@actionName", 
                        properties.resource.actionName != null ? properties.resource.actionName : properties.resource.triggerName));
                    // Run the stored procedure.
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                throw;
            }
        }
    }

    public class Properties
    {
        public string schema { get; set; }
        public DateTime? startTime { get; set; }
        public string status { get; set; }
        public Tags tags { get; set; }
        public Resource resource { get; set; }
        public Correlation correlation { get; set; }
        public DateTime? endTime { get; set; }
        public string code { get; set; }
        public Trackedproperties trackedProperties { get; set; }
        public Error error { get; set; }

    }

    public class Error
    {
        public string code { get; set; }
        public string message { get; set; }

        public void Save(string runId, SqlConnection sqlconn, string spName)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(spName, sqlconn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@runId", runId));
                    cmd.Parameters.Add(new SqlParameter("@code", code));
                    cmd.Parameters.Add(new SqlParameter("@message", message));
                    // Run the stored procedure.
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                throw;
            }
        }
    }

    public class Tags
    {
    }

    public class Resource
    {
        public string subscriptionId { get; set; }
        public string resourceGroupName { get; set; }
        public string workflowId { get; set; }
        public string workflowName { get; set; }
        public string runId { get; set; }
        public string location { get; set; }
        public string actionName { get; set; }
        public string triggerName { get; set; }
        public string originRunId { get; set; }
    }

    public class Correlation
    {
        public string actionTrackingId { get; set; }
        public string clientTrackingId { get; set; }
    }

    public class Trackedproperties
    {
        [JsonExtensionData]
        public IDictionary<string, object> items { get; set; }

        public void Save(string runId, SqlConnection sqlconn, string spName)
        {
            try
            {
                foreach (var key in items.Keys)
                {
                    using (SqlCommand cmd = new SqlCommand(spName, sqlconn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@runId", runId));
                        cmd.Parameters.Add(new SqlParameter("@propName", key));
                        object value = null;
                        if (items.TryGetValue(key, out value))
                            cmd.Parameters.Add(new SqlParameter("@propValue", Convert.ToString(value)));
                        // Run the stored procedure.
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
