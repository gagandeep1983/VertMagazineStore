using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VertMagazineStore
{

    class Program
    {
        static string baseURL = "http://magazinestore.azurewebsites.net/api";

        static void Main(string[] args)
        {
            try
            {
                //get token
                string token = getToken();
                if (token != "")
                {
                    //get all subscribiers
                    dynamic jsonSubscribers = null;
                    Thread threadGetAllSubscriber = new Thread(() => { jsonSubscribers = getAllSubscribers(token); });
                    threadGetAllSubscriber.Start();

                    //call API for get all categories
                    Task<string> categoryResponse = getResponse(baseURL + "/categories/" + token);
                    dynamic jsonCategories = JsonConvert.DeserializeObject(categoryResponse.Result);

                    var listCategories = new List<Categories>();

                    //create array for all categories
                    JArray arrCategory = (JArray)jsonCategories["data"];


                    Thread threadGetAllCategory = new Thread(() => getCategory(token, listCategories, arrCategory));
                    threadGetAllCategory.Start();
                    //get all subscribiers
                    //dynamic jsonSubscribers = getAllSubscribers(token);


                    //create list for subscribers
                    var listSubscribers = new List<string>();
                    threadGetAllSubscriber.Join();
                    threadGetAllCategory.Join();

                    foreach (var subscriber in jsonSubscribers.data)
                    {
                        int countMagazineUnderEachCategory = 0;

                        for (int i = 0; i < listCategories.Count; i++)
                        {
                            string strMagazineIds = subscriber.magazineIds.ToString();
                            string strCategoryIds = listCategories[i].magazineIds.Substring(0, listCategories[i].magazineIds.Length - 1);

                            // checking if any magazine match with category magazine
                            bool any = strCategoryIds.Split(',').Any(res => strMagazineIds.Contains(res));

                            if (any)
                            {
                                countMagazineUnderEachCategory += 1;

                                if (countMagazineUnderEachCategory == listCategories.Count)
                                {
                                    listSubscribers.Add(subscriber.id.ToString());
                                }
                            }
                        }

                    }

                    var subscribers = new
                    {
                        subscribers = listSubscribers
                    };

                    string strSubScriberIds = JsonConvert.SerializeObject(subscribers);


                    //POST Answer
                    dynamic jsonAnswer = postRequest(baseURL + "/answer/" + token, strSubScriberIds);

                    Console.WriteLine(jsonAnswer);
                    Console.ReadKey();
                }
                else
                {
                    Console.Write("Something wrong with token API!");
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }

        /// <summary>
        /// get all subscribers
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static dynamic getAllSubscribers(string token)
        {
            Task<string> subscriberResponse = getResponse(baseURL + "/subscribers/" + token);
            dynamic jsonSubscribers = JsonConvert.DeserializeObject(subscriberResponse.Result);
            return jsonSubscribers;
        }

        /// <summary>
        /// get all category
        /// </summary>
        /// <param name="token"></param>
        /// <param name="listCategories"></param>
        /// <param name="arrCategory"></param>
        private static void getCategory(string token, List<Categories> listCategories, JArray arrCategory)
        {
            foreach (var category in arrCategory)
            {
                var objCategories = new Categories();

                //call API for get all magazines under each category 
                Task<string> magazineResponse = getResponse(baseURL + "/magazines/" + token + "/" + category);
                dynamic jsonMagazines = JsonConvert.DeserializeObject(magazineResponse.Result);

                foreach (var magazine in jsonMagazines.data)
                {
                    //objCategories.categoryName = category;
                    objCategories.magazineIds += magazine.id + ",";
                };

                listCategories.Add(objCategories);

            }
        }


        //get token
        private static string getToken()
        {
            string token = "";
            try
            {
                Task<string> response = getResponse(baseURL + "/token");

                dynamic jsonTokenResponse = JsonConvert.DeserializeObject(response.Result);
                token = jsonTokenResponse.token;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            return token;
        }

        /// <summary>
        /// get response
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="requestType"></param>
        /// <returns></returns>
        private static async Task<string> getResponse(string URL)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            string strResponse = "";
            try
            {
                request.Method = WebRequestMethods.Http.Get;
                request.ContentLength = 0;
                request.ContentType = "application/json";

                request.AllowAutoRedirect = true;
                request.Proxy = null;
                request.AutomaticDecompression = DecompressionMethods.GZip;

                using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader streamreader = new StreamReader(responseStream))
                    {
                        strResponse = streamreader.ReadToEnd();
                    }
                }

            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
            return strResponse;
        }

        /// <summary>
        /// post request
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="requestType"></param>
        /// <param name="requestBody"></param>
        /// <returns></returns>
        private static dynamic postRequest(string URL, string requestBody)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            string strResponse = "";
            dynamic jsonResponse = null;
            try
            {
                request.Method = WebRequestMethods.Http.Post;
                request.ContentLength = 0;
                request.ContentType = "application/json";
                request.AllowAutoRedirect = true;
                request.Proxy = null;

                request.UseDefaultCredentials = true;
                request.PreAuthenticate = true;
                request.Credentials = CredentialCache.DefaultCredentials;


                byte[] postBytes = Encoding.UTF8.GetBytes(requestBody);

                request.ContentLength = postBytes.Length;
                Stream requestStream = request.GetRequestStream();

                // now send it
                requestStream.Write(postBytes, 0, postBytes.Length);
                requestStream.Close();

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader streamreader = new StreamReader(responseStream))
                    {
                        strResponse = streamreader.ReadToEnd();
                        jsonResponse = JsonConvert.DeserializeObject(strResponse);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
            return jsonResponse;
        }
    }
}