﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using JodelAPI.Objects;
using JodelAPI.Json;
using Newtonsoft.Json;

namespace JodelAPI
{
    public static class API
    {
        /// <summary>
        /// Colors for Jodels
        /// </summary>
        public enum PostColor
        {
            Orange,
            Yellow,
            Red,
            Blue,
            Bluegreyish,
            Green,
            Random
        }

        /// <summary>
        /// Decisions for flaging an Jodel
        /// </summary>
        public enum Decision
        {
            Allow = 0,
            Block = 2,
            DontKnow = 1
        }

        /// <summary>
        /// Unit for calculating distance
        /// </summary>
        public enum Unit
        {
            Kilometers,
            Meters,
            Miles
        }

        /// <summary>
        /// Reason for reporting Jodels
        /// </summary>
        public enum Reason
        {
            PersonalData = 1,
            Mobbing = 2,
            Spamming = 3,
            Spoiler = 4,
            Other = 5
        }


        public static string AccessToken = "";
        public static string Latitude = "";
        public static string Longitude = "";
        public static string CountryCode = "";
        public static string City = "";
        public static string GoogleApiToken = "";
        private static string _lastPostId = "";

        /// <summary>
        /// Gets the first amount of Jodels (internal usage)
        /// </summary>
        /// <returns>List&lt;Jodels&gt;.</returns>
        public static List<Jodels> GetFirstJodels()
        {
            string plainJson = GetPageContent(Constants.LinkFirstJodels.ToLink());
            JsonJodelsFirstRound.RootObject jfr = JsonConvert.DeserializeObject<JsonJodelsFirstRound.RootObject>(plainJson);
            List<Jodels> temp = new List<Jodels>(); // List<post_id,message>

            int i = 0;
            foreach (var item in jfr.recent)
            {
                string msg = item.message;
                bool isUrl = false;
                if (msg == "Jodel")
                {
                    msg = "http:" + item.image_url;
                    isUrl = true;
                }

                Jodels objJodels = new Jodels
                {
                    PostId = item.post_id,
                    Message = msg,
                    HexColor = item.color,
                    IsImage = isUrl,
                    VoteCount = item.vote_count,
                    Latitude = item.location.loc_coordinates.lat.ToString(),
                    Longitude = item.location.loc_coordinates.lng.ToString(),
                    LocationName = item.location.name
                };

                temp.Add(objJodels);

                i++;
            }

            _lastPostId = temp.Last().PostId; // Set the last post_id for next jodels

            return temp;
        }

        /// <summary>
        /// Gets the second amount of Jodels (internal usage)
        /// </summary>
        /// <returns>List&lt;Jodels&gt;.</returns>
        public static List<Jodels> GetNextJodels()
        {
            List<Jodels> temp = new List<Jodels>();
            for (int e = 0; e < 3; e++)
            {
                string plainJson = GetPageContent(Constants.LinkSecondJodels.ToLink(_lastPostId));
                JsonJodelsLastRound.RootObject jlr = JsonConvert.DeserializeObject<JsonJodelsLastRound.RootObject>(plainJson);
                int i = 0;
                foreach (var item in jlr.posts)
                {
                    string msg = item.message;
                    bool isUrl = false;
                    if (msg == "Jodel")
                    {
                        msg = "http:" + item.image_url; // WELL THERE IS NO IMAGE_URL!!!!???
                        isUrl = true;
                    }

                    Jodels objJodels = new Jodels
                    {
                        PostId = item.post_id,
                        Message = msg,
                        HexColor = item.color,
                        IsImage = isUrl,
                        VoteCount = item.vote_count,
                        Latitude = item.location.loc_coordinates.lat.ToString(),
                        Longitude = item.location.loc_coordinates.lng.ToString(),
                        LocationName = item.location.name
                    };

                    temp.Add(objJodels);
                    i++;
                }

                _lastPostId = temp.Last().PostId; // Set the last post_id for next jodels
            }
            return temp;
        }

        /// <summary>
        /// Gets all jodels.
        /// </summary>
        /// <returns>List&lt;Jodels&gt;.</returns>
        public static List<Jodels> GetAllJodels()
        {
            var allJodels = GetFirstJodels();
            allJodels.AddRange(GetNextJodels());
            return allJodels;
        }

        /// <summary>
        /// Upvotes the specified post identifier (Jodel).
        /// </summary>
        /// <param name="postId">The post identifier.</param>
        public static void Upvote(string postId)
        {
            DateTime dt = DateTime.UtcNow;

            string stringifiedPayload =
                @"PUT%api.go-tellm.com%443%/api/v2/posts/" + postId + "/" + "upvote/%" + AccessToken + "%" + $"{dt:s}Z" + "%%";

            var keyByte = Encoding.UTF8.GetBytes(Constants.Key);
            var hmacsha1 = new HMACSHA1(keyByte);
            hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(stringifiedPayload));

            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                client.Headers.Add("User-Agent", "Jodel/4.13.2 Dalvik/2.1.0 (Linux; U; Android 6.0.1; Nexus 5 Build/MMB29V)"); //TODO: Randomize
                client.Headers.Add("Accept-Encoding", "gzip");
                client.Headers.Add("X-Client-Type", "android_4.13.2");
                client.Headers.Add("X-Api-Version", "0.2");
                client.Headers.Add("X-Timestamp", $"{dt:s}Z");
                client.Headers.Add("X-Authorization", "HMAC " + ByteToString(hmacsha1.Hash));
                client.Headers.Add("Authorization", "Bearer " + AccessToken);
                client.Encoding = Encoding.UTF8;
                client.UploadData(Constants.LinkUpvoteJodel.ToLink(postId), "PUT", new byte[] { });
            }
        }

        /// <summary>
        /// Downvotes the specified post identifier (Jodel).
        /// </summary>
        /// <param name="postId">The post identifier.</param>
        public static void Downvote(string postId)
        {
            DateTime dt = DateTime.UtcNow;

            string stringifiedPayload =
                @"PUT%api.go-tellm.com%443%/api/v2/posts/" + postId + "/" + "downvote/%" + AccessToken + "%" + $"{dt:s}Z" + "%%";

            var keyByte = Encoding.UTF8.GetBytes(Constants.Key);
            var hmacsha1 = new HMACSHA1(keyByte);
            hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(stringifiedPayload));

            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                client.Headers.Add("User-Agent", "Jodel/4.13.2 Dalvik/2.1.0 (Linux; U; Android 6.0.1; Nexus 5 Build/MMB29V)"); //TODO: Randomize
                client.Headers.Add("Accept-Encoding", "gzip, deflate");
                client.Headers.Add("X-Client-Type", "android_4.13.2");
                client.Headers.Add("X-Api-Version", "0.2");
                client.Headers.Add("X-Timestamp", $"{dt:s}Z");
                client.Headers.Add("X-Authorization", "HMAC " + ByteToString(hmacsha1.Hash));
                client.Headers.Add("Authorization", "Bearer " + AccessToken);
                client.Encoding = Encoding.UTF8;
                client.UploadData(Constants.LinkDownvoteJodel.ToLink(postId), "PUT", new byte[] { });
            }
        }

        /// <summary>
        /// Gets the karma.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int GetKarma()
        {
            string resp = GetPageContent(Constants.LinkGetKarma.ToLink());
            string result = resp.Substring(resp.LastIndexOf(':') + 1);
            return Convert.ToInt32(result.Replace("}", "").Replace("\"", ""));
        }

        /// <summary>
        /// Posts an jodel.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="colorParam">The color parameter.</param>
        /// <param name="postID">The post identifier.</param>
        public static void PostJodel(string message, PostColor colorParam = PostColor.Random, string postID = null)
        {
            DateTime dt = DateTime.UtcNow;

            var color = GetColor(colorParam);

            string jsonCommentFragment = String.Empty;
            if (postID != null)
            {
                jsonCommentFragment = @"""ancestor"": """ + postID + @""", ";
            }

            string stringifiedPayload = @"POST%api.go-tellm.com%443%/api/v2/posts/%" + AccessToken + "%" + $"{dt:s}Z" +
                                        @"%%{""color"": """ + color + @""", " + jsonCommentFragment +
                                        @"""message"": """ + message + @""", ""location"": {""loc_accuracy"": 1, ""city"": """ + City +
                                        @""", ""loc_coordinates"": {""lat"": " + Latitude + @", ""lng"": " + Longitude +
                                        @"}, ""country"": """ + CountryCode + @""", ""name"": """ + City + @"""}}";

            string payload = @"{""color"": """ + color + @""", " + jsonCommentFragment +
                             @"""message"": """ + message + @""", ""location"": {""loc_accuracy"": 1, ""city"": """ + City +
                             @""", ""loc_coordinates"": " + @"{""lat"": " + Latitude + @", ""lng"": " + Longitude +
                             @"}, ""country"": """ + CountryCode + @""", ""name"": """ + City + @"""}}";

            var keyByte = Encoding.UTF8.GetBytes(Constants.Key);
            using (var hmacsha1 = new HMACSHA1(keyByte))
            {
                hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(stringifiedPayload));

                GetPageContentPost(Constants.LinkPostJodel, payload, true, ByteToString(hmacsha1.Hash), $"{dt:s}Z");
            }
        }

        /// <summary>
        /// Gets the comments.
        /// </summary>
        /// <param name="postId">The post identifier.</param>
        /// <returns>List&lt;Comments&gt;.</returns>
        public static List<Comments> GetComments(string postId)
        {
            string plainJson = GetPageContent(Constants.LinkGetComments.ToLink());
            JsonComments.RootObject com = JsonConvert.DeserializeObject<JsonComments.RootObject>(plainJson);

            return com.children.Select(c => new Comments()
            {
                PostId = c.post_id,
                Message = c.message,
                UserHandle = c.user_handle,
                VoteCount = c.vote_count
            }).ToList();
        }

        /// <summary>
        /// Gets the reported Jodels
        /// </summary>
        /// <returns>List&lt;ModerationQueue&gt;.</returns>
        public static List<ModerationQueue> GetModerationQueue()
        {
            string plainJson = GetPageContent(Constants.LinkModeration.ToLink());
            JsonModeration.RootObject queue = JsonConvert.DeserializeObject<JsonModeration.RootObject>(plainJson);
            return queue.posts.Select(item => new ModerationQueue()
            {
                PostId = item.post_id,
                FlagCount = item.flag_count,
                FlagReason = item.flag_reason,
                HexColor = item.color,
                Message = item.message,
                ParentId = item.parent_id,
                TaskId = item.task_id,
                UserHandle = item.user_handle,
                VoteCount = item.vote_count

            }).ToList();
        }

        /// <summary>
        /// Generates an access token.
        /// </summary>
        /// <returns>System.String.</returns>
        public static string GenerateAccessToken()
        {
            DateTime dt = DateTime.UtcNow;

            string deviceUid = Sha256(RandomString(5, true));

            string stringifiedPayload = @"POST%api.go-tellm.com%443%/api/v2/users/%%" + $"{dt:s}Z" +
                                        @"%%{""device_uid"": """ + deviceUid + @""", ""location"": {""city"": """ + City +
                                        @""", ""loc_accuracy"": 100, ""loc_coordinates"": {""lat"": " + Latitude +
                                        @", ""lng"": " + Longitude + @"}, ""country"": """ + CountryCode + @"""}, " +
                                        @"""client_id"": """ + Constants.ClientId + @"""}";

            string payload = @"{""device_uid"": """ + deviceUid + @""", ""location"": {""city"": """ + City +
                             @""", ""loc_accuracy"": 100, ""loc_coordinates"": " + @"{""lat"": " + Latitude +
                             @", ""lng"": " + Longitude + @"}, ""country"": """ + CountryCode +
                             @"""}, ""client_id"": """ + Constants.ClientId + @"""}";

            var keyByte = Encoding.UTF8.GetBytes(Constants.Key);
            using (var hmacsha1 = new HMACSHA1(keyByte))
            {
                hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(stringifiedPayload));

                return GetPageContentPost(Constants.LinkGenAT, payload, false,
                    ByteToString(hmacsha1.Hash), $"{dt:s}Z");
            }
        }

        /// <summary>
        /// Flags the jodel.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="decision">The decision.</param>
        public static void FlagJodel(int taskId, Decision decision)
        {
            DateTime dt = DateTime.UtcNow;

            string dec = Convert.ChangeType(decision, decision.GetTypeCode())?.ToString(); // get int from enum.
            string stringifiedPayload = @"{""decision"": " + dec +
                                        @", ""task_id"": """ + taskId +
                                        @"""}";


            var keyByte = Encoding.UTF8.GetBytes(Constants.Key);
            var hmacsha1 = new HMACSHA1(keyByte);
            hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(stringifiedPayload));

            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                client.Headers.Add("User-Agent", "Jodel/4.13.2 Dalvik/2.1.0 (Linux; U; Android 6.0.1; Nexus 5 Build/MMB29V)"); // TODO: Randomize
                client.Headers.Add("Accept-Encoding", "gzip");
                client.Headers.Add("X-Client-Type", "android_4.13.2");
                client.Headers.Add("X-Api-Version", "0.2");
                client.Headers.Add("X-Timestamp", $"{dt:s}Z");
                client.Headers.Add("X-Authorization", "HMAC " + ByteToString(hmacsha1.Hash));
                client.Encoding = Encoding.UTF8;
                client.UploadString(Constants.LinkModeration.ToLink(), stringifiedPayload);
            }
        }

        /// <summary>
        /// Reports the jodel.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="decision">The decision.</param>
        public static void ReportJodel(string postId, Reason reason)
        {
            DateTime dt = DateTime.UtcNow;

            string rea = Convert.ChangeType(reason, reason.GetTypeCode())?.ToString(); // get int from enum.
            string stringifiedPayload = @"{""reason_id"":"+rea+"}";

            var keyByte = Encoding.UTF8.GetBytes(Constants.Key);
            var hmacsha1 = new HMACSHA1(keyByte);
            hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(stringifiedPayload));

            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                client.Headers.Add("User-Agent", "Jodel/4.13.2 Dalvik/2.1.0 (Linux; U; Android 6.0.1; Nexus 5 Build/MMB29V)");
                client.Headers.Add("Accept-Encoding", "gzip");
                client.Headers.Add("X-Client-Type", "android_4.13.2");
                client.Headers.Add("X-Api-Version", "0.2");
                client.Headers.Add("X-Timestamp", $"{dt:s}Z");
                client.Headers.Add("X-Authorization", "HMAC " + ByteToString(hmacsha1.Hash));
                client.Headers.Add("Authorization", "Bearer " + AccessToken);
                client.Encoding = Encoding.UTF8;
                client.UploadData(Constants.LinkReportJodel.ToLink(postId), "PUT", new byte[] { });
            }
        }

        /// <summary>
        /// Filters the Jodels by a string.
        /// </summary>
        /// <param name="jodels">The jodels.</param>
        /// <param name="channel">The name.</param>
        /// <returns>List&lt;Jodels&gt;.</returns>
        public static List<Jodels> FilterByChannel(List<Jodels> jodels, string channel) // Get's all jodels containing the word
        {
            if (channel[0] == '#')
            {
                channel = channel.Remove(0, 1);
            }

            List<Jodels> temp = (
                from jodel in jodels
                where jodel.Message.Contains(channel)
                select new Jodels()
                {
                    PostId = jodel.PostId, HexColor = jodel.HexColor, IsImage = jodel.IsImage, Latitude = jodel.Latitude, Longitude = jodel.Longitude, LocationName = jodel.LocationName, Message = jodel.Message, VoteCount = jodel.VoteCount
                }).ToList();

            return temp;
        }

        /// <summary>
        /// Gets the coordinates.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>Coordinates.</returns>
        public static Coordinates GetCoordinates(string location)
        {
            return GetCoords(location);
        }

        /// <summary>
        /// Sets the location.
        /// </summary>
        /// <param name="location">The location.</param>
        public static void SetLocation(string location)
        {
            var coord = GetCoords(location);

            Latitude = coord.Latitude;
            Longitude = coord.Longitude;
        } // from location name via Google API

        /// <summary>
        /// Sets the location.
        /// </summary>
        /// <param name="coord">The coord.</param>
        public static void SetLocation(Coordinates coord)
        {
            Latitude = coord.Latitude;
            Longitude = coord.Longitude;
        } // from created object

        /// <summary>
        /// Calculates the distance.
        /// </summary>
        /// <param name="coord1">The coord1.</param>
        /// <param name="coord2">The coord2.</param>
        /// <param name="unit">The unit.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="InternalException">API Error: Calculating Distance</exception>
        public static double CalcDistance(Coordinates coord1, Coordinates coord2, Unit unit)
        {
            double c1lo = double.Parse(coord1.Longitude, System.Globalization.CultureInfo.InvariantCulture);
            double c2lo = double.Parse(coord2.Longitude, System.Globalization.CultureInfo.InvariantCulture);
            double c1la = double.Parse(coord1.Latitude, System.Globalization.CultureInfo.InvariantCulture);
            double c2la = double.Parse(coord2.Latitude, System.Globalization.CultureInfo.InvariantCulture);

            switch (unit)
            {
                case Unit.Kilometers:
                    return Distance.KilometresBetweenTwoGeographicCoordinates(c1lo, c1la, c2lo, c2la);
                case Unit.Meters:
                    return Distance.MetresBetweenTwoGeographicCoordinates(c1lo, c1la, c2lo, c2la);
                case Unit.Miles:
                    return Distance.MilesBetweenTwoGeographicCoordinates(c1lo, c1la, c2lo, c2la);
                default:
                    throw new InternalException("API Error: Calculating Distance");
            }
        }

        /// <summary>
        /// Gets my jodels.
        /// </summary>
        /// <returns>List&lt;MyJodels&gt;.</returns>
        public static List<MyJodels> GetMyJodels()
        {
            string plainJson = GetPageContent(Constants.LinkGetMyJodels.ToLink());

            JsonMyJodels.RootObject myJodels = JsonConvert.DeserializeObject<JsonMyJodels.RootObject>(plainJson);
            return myJodels.posts.Select(item => new MyJodels()
            {
                PostId = item.post_id,
                Message = item.message,
                HexColor = item.color,
                VoteCount = item.vote_count,
                Latitude = item.location.loc_coordinates.lat.ToString(),
                Longitude = item.location.loc_coordinates.lng.ToString(),
                LocationName = item.location.name
            }).ToList();
        }

        /// <summary>
        /// Gets my comments.
        /// </summary>
        /// <returns>List&lt;MyComments&gt;.</returns>
        public static List<MyComments> GetMyComments()
        {
            string plainJson = GetPageContent(Constants.LinkGetMyComments.ToLink());

            JsonMyComments.RootObject myComments = JsonConvert.DeserializeObject<JsonMyComments.RootObject>(plainJson);
            return myComments.posts.Select(item => new MyComments()
            {
                PostId = item.post_id,
                Message = item.message,
                HexColor = item.color,
                VoteCount = item.vote_count,
                IsOwn = item.post_own.Equals("own"),
                Latitude = item.location.loc_coordinates.lat.ToString(),
                Longitude = item.location.loc_coordinates.lng.ToString(),
                LocationName = item.location.name
            }).ToList();
        }

        /// <summary>
        /// Gets my votes.
        /// </summary>
        /// <returns>List&lt;MyVotes&gt;.</returns>
        public static List<MyVotes> GetMyVotes()
        {
            string plainJson = GetPageContent(Constants.LinkGetMyVotes.ToLink());

            JsonMyVotes.RootObject myVotes = JsonConvert.DeserializeObject<JsonMyVotes.RootObject>(plainJson);
            return myVotes.posts.Select(item => new MyVotes()
            {
                PostId = item.post_id,
                Message = item.message,
                HexColor = item.color,
                VoteCount = item.vote_count,
                IsOwn = item.post_own.Equals("own"),
                LocationName = item.location.name
            }).ToList();
        }

        /// <summary>
        /// Determines whether the specified token is from a moderator.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns><c>true</c> if the specified token is moderator; otherwise, <c>false</c>.</returns>
        public static bool IsModerator(string token)
        {
            string plainJson = GetPageContent(Constants.LinkConfig.ToLink());

            JsonConfig.RootObject config = JsonConvert.DeserializeObject<JsonConfig.RootObject>(plainJson);
            
            if(config.moderator)
            {
                return true;
            }
            return false;
        }


        private static string ByteToString(byte[] buff)
        {
            return buff.Aggregate("", (current, t) => current + t.ToString("X2"));
        }

        private static string GetPageContent(string link)
        {
            string html;
            WebRequest request = WebRequest.Create(link);
            WebResponse response = request.GetResponse();
            Stream data = response.GetResponseStream();
            using (StreamReader sr = new StreamReader(data))
            {
                html = sr.ReadToEnd();
            }
            return html;
        }

        private static string GetPageContentPost(string link, string post, bool bearer, string hmac, string timestamp)
        {
            var request = (HttpWebRequest)WebRequest.Create(link);

            var data = Encoding.UTF8.GetBytes(post);

            request.Method = "POST";
            request.ContentType = "application/json; charset=UTF-8";
            request.ContentLength = data.LongLength;
            request.UserAgent = "Jodel/4.13.2 Dalvik/2.1.0 (Linux; U; Android 6.0.1; Nexus 5 Build/MMB29V)"; //TODO: Randomize
            request.KeepAlive = true;
            request.Headers.Add("Accept-Encoding", "gzip");
            request.Headers.Add("X-Client-Type", "android_4.13.2");
            request.Headers.Add("X-Api-Version", "0.2");
            if (timestamp != null)
                request.Headers.Add("X-Timestamp", timestamp);
            if (hmac != null)
                request.Headers.Add("X-Authorization", "HMAC " + hmac);

            if (bearer)
            {
                request.Headers.Add("Authorization", "Bearer " + AccessToken);
            }
            request.ServicePoint.Expect100Continue = false;
            request.AuthenticationLevel = AuthenticationLevel.None;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            if (hmac != null)
            {
                var responseJson = JsonConvert.DeserializeObject<dynamic>(responseString); // ugly solution, may throw exception if no access token is responded
                responseString = responseJson.access_token;
            }
            return responseString;
        }

        private static string Sha256(string value)
        {
            StringBuilder sb = new StringBuilder();

            using (SHA256 hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(value));

                foreach (byte b in result)
                    sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        private static string RandomString(int size, bool lowerCase)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            for (int i = 1; i < size + 1; i++)
            {
                var ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }

        private static string GetColor(PostColor c)
        {
            switch (c)
            {
                case PostColor.Red:
                    return "DD5F5F";
                case PostColor.Orange:
                    return "FF9908";
                case PostColor.Yellow:
                    return "FFBA00";
                case PostColor.Blue:
                    return "DD5F5F";
                case PostColor.Bluegreyish:
                    return "8ABDB0";
                case PostColor.Green:
                    return "9EC41C";
                case PostColor.Random:
                    return "FFFFFF";
                default:
                    throw new ArgumentOutOfRangeException(nameof(c), c, null);
            }
        }

        private static Coordinates GetCoords(string address)
        {
            string[] coords = address.ToCoordinates();
            Coordinates coord = new Coordinates
            {
                Latitude = coords[0],
                Longitude = coords[1]
            };

            return coord;
        }

        private static string ToLink(this string link)
        {
            if(link.Contains("{AT}"))
            {
                link = link.Replace("{AT}", AccessToken);
            }

            if(link.Contains("{LAT}"))
            {
                link = link.Replace("{LAT}", Latitude);
            }

            if(link.Contains("{LNG}"))
            {
                link = link.Replace("{LNG}", Longitude);
            }

            return link;
        }

        private static string ToLink(this string link, string postId)
        {
            if (link.Contains("{AT}"))
            {
                link = link.Replace("{AT}", AccessToken);
            }

            if (link.Contains("{LAT}"))
            {
                link = link.Replace("{LAT}", Latitude);
            }

            if (link.Contains("LNG"))
            {
                link = link.Replace("{LNG}", Longitude);
            }

            if(link.Contains("{PID}"))
            {
                link = link.Replace("{PID}", postId);
            }

            return link;
        }
    }
}