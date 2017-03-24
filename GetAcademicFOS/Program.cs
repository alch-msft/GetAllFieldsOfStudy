using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace GetAcademicFOS
{
    class Program
    {
        static void Main(string[] args)
        {
            //Getting all FOS Ids
            const string FosIdsRequest = @"https://westus.api.cognitive.microsoft.com/academic/v1.0/evaluate?attributes=Id&expr=Ty='6'&count=60000";       
            const string EntityDetailsByIdRequestTemplate = @"https://westus.api.cognitive.microsoft.com/academic/v1.0/evaluate?attributes=*&expr=Id={0}";
            const string OutputRoot = @"C:\Users\alch\Desktop\FOS\";
            const string APIKey = @"Your own api key!"; //Put your own api key here!

            HttpWebRequest fosWebRequest = (HttpWebRequest)WebRequest.Create(FosIdsRequest);
            fosWebRequest.Headers["Ocp-Apim-Subscription-Key"] = APIKey;

            string fosResponseStr = (new StreamReader(fosWebRequest.GetResponse().GetResponseStream())).ReadToEnd();
            JObject jfosResponse = (JObject)JsonConvert.DeserializeObject(fosResponseStr);
            
            List<Int64> fosIds = new List<Int64>();

            foreach (JObject fos in jfosResponse["entities"].Children())
            {
                fosIds.Add(fos["Id"].ToObject<Int64>());
            }
            Parallel.ForEach(fosIds, (Fid) =>
            {
                string fosDetailRequeststr = string.Format(EntityDetailsByIdRequestTemplate, Fid);
                HttpWebRequest fosDetailWebRequest = (HttpWebRequest)WebRequest.Create(fosDetailRequeststr);
                fosDetailWebRequest.Headers["Ocp-Apim-Subscription-Key"] = APIKey;

                string fosDetailResponseStr = (new StreamReader(fosDetailWebRequest.GetResponse().GetResponseStream())).ReadToEnd();
                JObject jfosDetailResponse = (JObject)JsonConvert.DeserializeObject(fosDetailResponseStr);

                foreach (JObject fosDetail in jfosDetailResponse["entities"].Children())
                {
                    string filename = (fosDetail["FN"] == null ? "" : fosDetail["FN"].ToObject<string>()) + "_" + fosDetail["Id"].ToObject<string>();
                    string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                    Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
                    filename = r.Replace(filename, "");

                    using (var sw = new StreamWriter(OutputRoot + filename + ".json"))
                    {
                        sw.Write(fosDetail.ToString());
                    }

                }
            });

        }
    }
}
