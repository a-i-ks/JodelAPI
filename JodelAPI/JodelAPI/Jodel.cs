﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JodelAPI.Json;
using JodelAPI.Objects;
using Newtonsoft.Json;

namespace JodelAPI
{
    public class Jodel
    {
        private static User _user;
        public Moderation Moderation { get; private set; }
        public Account Account { get; private set; }
        public Location Location { get; private set; }

        public Jodel(string accessToken, string longitude, string latitude, string city, string countryCode, string googleApiToken = "")
            : this(new User(accessToken, latitude, longitude, countryCode, city, googleApiToken)) { }

        public Jodel(User user)
        {
            _user = user;
            Helpers._user = user;
            Moderation = new Moderation();
            Account = new Account(user);
            Location = new Location(user);
        }

        /// <summary>
        ///     Colors for Jodels
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
        ///     Methods to sort List&lt;Jodels%gt;
        /// </summary>
        public enum SortMethod
        {
            MostCommented,
            Top
        }

        /// <summary>
        /// Gets the UserConfig
        /// </summary>
        public User.UserConfig GetUserConfig()
        {
            string plainJson = Constants.GetConfig.ExecuteRequest();

            JsonConfig.RootObject config = JsonConvert.DeserializeObject<JsonConfig.RootObject>(plainJson);

            List<User.Experiment> experiments = new List<User.Experiment>(config.experiments.Count);
            foreach (JsonConfig.Experiment experiment in config.experiments)
            {
                experiments.Add(new User.Experiment
                {
                    Features = experiment.features,
                    Group = experiment.group,
                    Name = experiment.name
                });
            }

            List<Channel> channels = new List<Channel>(config.followed_hashtags.Count);
            foreach (string channelname in config.followed_channels)
            {
                channels.Add(new Channel(channelname));
            }

            User.UserConfig uconfig = new User.UserConfig
            {
                ChannelsFollowLimit = config.channels_follow_limit,
                Experiments = experiments,
                HomeName = config.home_name,
                HomeSet = config.home_set,
                FollowedHashtags = config.followed_hashtags,
                Location = config.location,
                Moderator = config.moderator,
                TripleFeedEnabled = config.triple_feed_enabled,
                UserType = config.user_type,
                Verified = config.verified,
                FollowedChannels = channels
            };
            _user.Config = uconfig;
            return uconfig;
        }

        /// <summary>
        /// Initial Load of all Followed Channels
        /// </summary>
        public void LoadFollowedChannels()
        {
            if (_user.Config == null)
                return;

            DateTime dt = DateTime.UtcNow;
            string jsonString;


            string payload = "{";

            List<string> channelnames = _user.Config.FollowedChannels.Select(x => x.ChannelName).ToList();
            if (channelnames.Count != 0)
            {
                for (int i = 0; i < channelnames.Count; i++)
                {
                    channelnames[i] = @"""" + channelnames[i] + @""":-1";
                }
                payload += channelnames.Aggregate((i, j) => i + "," + j);
            }


            payload += "}";


            string stringifiedPayload = "POST%api.go-tellm.com%443%/api/v3/user/followedChannelsMeta%%" + $"{dt:s}Z" + "%%" + payload;

            var keyByte = Encoding.UTF8.GetBytes(Constants.Key);
            using (var hmacsha1 = new HMACSHA1(keyByte))
            {
                hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(stringifiedPayload));

                using (var client = new MyWebClient())
                {
                    client.Headers.Add(Constants.Header.ToHeader(stringifiedPayload, DateTime.UtcNow));
                    client.Encoding = Encoding.UTF8;
                    jsonString = client.UploadString(Constants.LinkLoadFollowedChannels.ToLink(), payload);
                }
            }

            //TODO: Channels laden
        }

        /// <summary>
        /// Gets 10 Jodels
        /// </summary>
        /// <param name="lastPostId">Post ID of last loaded post, starting at next post</param>
        /// <returns>List&lt;Jodels&gt;.</returns>
        public List<Jodels> GetJodels(string lastPostId = null)
        {
            // parameters
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if(!string.IsNullOrWhiteSpace(lastPostId)) parameters.Add("after", lastPostId);
            parameters.Add("lat", _user.Latitude);
            parameters.Add("lng", _user.Longitude);

            // get json from jodel api
            string plainJson;
            if(lastPostId == null) plainJson = Constants.GetCombo.ExecuteRequest(parameters: parameters);
            else plainJson = Constants.GetPosts.ExecuteRequest(parameters: parameters);
            // deserialize json
            JsonJodels.RootObject jfr = JsonConvert.DeserializeObject<JsonJodels.RootObject>(plainJson);
            // create jodels objects
            if (jfr.recent != null) return jfr.recent.Select(j => new Jodels(j)).ToList();
            if (jfr.posts != null) return jfr.posts.Select(j => new Jodels(j)).ToList();
            return new List<Jodels>();
        }

        /// <summary>
        ///     Gets all jodels.
        /// </summary>
        /// <param name="limit">Limit of Jodels to load, Standard: 150</param>
        /// <returns>List&lt;Jodels&gt;.</returns>
        public List<Jodels> GetAllJodels(int limit = 150)
        {
            List<Jodels> allJodels = new List<Jodels>();
            List<Jodels> nextJodels = GetJodels();
            allJodels.AddRange(nextJodels);

            while (nextJodels.Count > 0 && allJodels.Count < limit)
            {
                nextJodels = GetJodels(allJodels.Last().PostId);
                allJodels.AddRange(nextJodels);
            }
            
            return allJodels;
        }

        /// <summary>
        ///     Upvotes the specified post identifier (Jodel).
        /// </summary>
        /// <param name="postId">The post identifier.</param>
        public void Upvote(string postId)
        {
            Constants.Upvote.ExecuteRequest(authToken: _user.AccessToken, postId: postId);
        }

        /// <summary>
        ///     Downvotes the specified post identifier (Jodel).
        /// </summary>
        /// <param name="postId">The post identifier.</param>
        public void Downvote(string postId)
        {
            Constants.Downvote.ExecuteRequest(authToken: _user.AccessToken, postId: postId);
        }

        /// <summary>
        ///     Posts a jodel.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="colorParam">The color parameter.</param>
        /// <param name="postId">The post identifier.</param>
        public string PostJodel(string message, PostColor colorParam = PostColor.Random, string postId = null)
        {
            string color = Helpers.GetColor(colorParam);

            string json = @"{ ""color"": """ + color + @""", ";
            if (postId != null)
            {
                json += @"""ancestor"": """ + postId + @""", ";
            }
            json += @"""message"": """ + message + @""", ""location"": {""loc_accuracy"": 1, ""city"": """ + _user.City +
                    @""", ""loc_coordinates"": {""lat"": " + _user.Latitude + @", ""lng"": " + _user.Longitude +
                    @"}, ""country"": """ + _user.CountryCode + @""", ""name"": """ + _user.City + @"""}}";

            string newJodel = Constants.NewPost.ExecuteRequest(authToken: _user.AccessToken, payload: json);
            JsonPostJodels.RootObject temp = JsonConvert.DeserializeObject<JsonPostJodels.RootObject>(newJodel);
            return temp.posts[0].post_id;
        }

        /// <summary>
        ///     Gets the comments.
        /// </summary>
        /// <param name="postId">The post identifier.</param>
        /// <returns>List&lt;Comments&gt;.</returns>
        public List<Comments> GetComments(string postId)
        {
            string plainJson = Constants.GetPost.ExecuteRequest(authToken: _user.AccessToken, postId: postId);
            JsonComments.RootObject com = JsonConvert.DeserializeObject<JsonComments.RootObject>(plainJson);
            if(com == null || com.children == null) return new List<Comments>();
            return com.children.Select(c => new Comments(c)).ToList();
        }

        /// <summary>
        ///     Gets my jodels.
        /// </summary>
        /// <returns>List&lt;MyJodels&gt;.</returns>
        public List<MyJodels> GetMyJodels()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("limit", "150");
            parameters.Add("access_token", _user.AccessToken);
            parameters.Add("skip", "0");
            string plainJson = Constants.GetMyCombo.ExecuteRequest(parameters: parameters);

            JsonMyJodels.RootObject myJodels = JsonConvert.DeserializeObject<JsonMyJodels.RootObject>(plainJson);
            return myJodels.posts.Select(item => new MyJodels(item)).ToList();
        }

        /// <summary>
        ///     Gets my comments.
        /// </summary>
        /// <returns>List&lt;MyComments&gt;.</returns>
        public List<MyComments> GetMyComments()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("limit", "150");
            parameters.Add("access_token", _user.AccessToken);
            parameters.Add("skip", "0");
            string plainJson = Constants.GetMyReplies.ExecuteRequest(parameters: parameters);

            JsonMyComments.RootObject myComments = JsonConvert.DeserializeObject<JsonMyComments.RootObject>(plainJson);
            return myComments.posts.Select(item => new MyComments
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
        ///     Gets my votes.
        /// </summary>
        /// <returns>List&lt;MyVotes&gt;.</returns>
        public List<MyVotes> GetMyVotes()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("limit", "150");
            parameters.Add("access_token", _user.AccessToken);
            parameters.Add("skip", "0");
            string plainJson = Constants.GetMyReplies.ExecuteRequest(parameters: parameters);

            JsonMyVotes.RootObject myVotes = JsonConvert.DeserializeObject<JsonMyVotes.RootObject>(plainJson);
            return myVotes.posts.Select(item => new MyVotes
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
        ///     Sorts the jodels.
        /// </summary>
        /// <param name="jodels">The jodels.</param>
        /// <param name="method">The method.</param>
        /// <returns>List&lt;Jodels&gt;.</returns>
        public List<Jodels> Sort(List<Jodels> jodels, SortMethod method)
        {
            return method == SortMethod.MostCommented
                ? jodels.OrderByDescending(o => o.CommentsCount).ToList()
                : jodels.OrderByDescending(o => o.VoteCount).ToList();
        }

        /// <summary>
        ///     Reports the jodel.
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="reason"></param>
        public void ReportJodel(string postId, Moderation.Reason reason)
        {
            string rea = Convert.ChangeType(reason, reason.GetTypeCode())?.ToString(); // get int from enum.
            string stringifiedPayload = @"{""reason_id"":" + rea + "}";

            using (var client = new MyWebClient())
            {
                client.Headers.Add(Constants.Header.ToHeader(stringifiedPayload, DateTime.UtcNow, true));
                client.Encoding = Encoding.UTF8;
                client.UploadData(Constants.LinkReportJodel.ToLink(_user.AccessToken, postId), "PUT", new byte[] { });
            }
        }

        /// <summary>
        ///     Pins a Jodel.
        /// </summary>
        /// <param name="postId"></param>
        public void PinJodel(string postId)
        {
            DateTime dt = DateTime.UtcNow;

            string stringifiedPayload =
                @"PUT%api.go-tellm.com%443%/api/v2/posts/" + postId + "/" + "pin?access_token=/%" + _user.AccessToken +
                "%" + $"{dt:s}Z" + "%%";

            using (var client = new MyWebClient())
            {
                client.Headers.Add(Constants.Header.ToHeader(stringifiedPayload, DateTime.UtcNow));
                client.Encoding = Encoding.UTF8;
                client.UploadData(Constants.LinkPinJodel.ToLink(_user.AccessToken, postId), "PUT", new byte[] { });
            }
        }

        /// <summary>
        ///     Get's all pinned Jodels.
        /// </summary>
        /// <returns>List&lt;MyPins&gt;.</returns>
        public List<MyPins> GetMyPins()
        {
            string plainJson;
            using (var client = new MyWebClient())
            {
                client.Encoding = Encoding.UTF8;
                plainJson = client.DownloadString(Constants.LinkMyPins.ToLink());
            }

            JsonMyPins.RootObject myPins = JsonConvert.DeserializeObject<JsonMyPins.RootObject>(plainJson);
            return myPins.posts.Select(item => new MyPins
            {
                PostId = item.post_id,
                Message = item.message,
                VoteCount = item.vote_count,
                PinCount = item.pin_count,
                IsOwn = item.post_own.Equals("own")
            }).ToList();
        }

        public void DeleteJodel(string postId)
        {
            DateTime dt = DateTime.UtcNow;

            string stringifiedPayload =
                @"PUT%api.go-tellm.com%443%/api/v2/posts/" + postId + "%" + _user.AccessToken + "%" + $"{dt:s}Z" +
                "%%";

            using (var client = new MyWebClient())
            {
                client.Headers.Add(Constants.Header.ToHeader(stringifiedPayload, DateTime.UtcNow, true));
                client.Encoding = Encoding.UTF8;
                client.UploadData(Constants.LinkDeleteJodel.ToLink(_user.AccessToken, postId), "DELETE", new byte[] { });
            }
        }

        /// <summary>
        ///     Get's the recommended channels.
        /// </summary>
        /// <returns>List&lt;RecommendedChannel&gt;.</returns>
        public List<RecommendedChannel> GetRecommendedChannels()
        {
            string plainJson;
            using (var client = new MyWebClient())
            {
                client.Encoding = Encoding.UTF8;
                plainJson = client.DownloadString(Constants.LinkGetRecommendedChannels.ToLink());
            }

            JsonRecommendedChannels.RootObject recommendedChannels =
                JsonConvert.DeserializeObject<JsonRecommendedChannels.RootObject>(plainJson);
            return recommendedChannels.recommended.Select(item => new RecommendedChannel
            {
                Name = item.channel,
                Followers = item.followers
            }).ToList();
        }

        public class Channel
        {
            public readonly string ChannelName;

            public Channel(string channelname)
            {
                if (channelname[0] == '#')
                    channelname = channelname.Remove(0, 1);
                ChannelName = channelname;
            }

            /// <summary>
            ///     Follows a channel.
            /// </summary>
            /// <param name="channel"></param>
            //public void FollowChannel()
            //{
            //    DateTime dt = DateTime.UtcNow;

            //    string stringifiedPayload =
            //        @"PUT%api.go-tellm.com%443%/api/v3/user/followChannel?access_token=" + _user.AccessToken + "%" +
            //        "&channel=" + ChannelName + $"{dt:s}Z" + "%%";

            //    using (var client = new MyWebClient())
            //    {
            //        client.Headers.Add(Constants.Header.ToHeader(stringifiedPayload, DateTime.UtcNow));
            //        client.Encoding = Encoding.UTF8;
            //        client.UploadData(Constants.LinkFollowChannel.ToLink(ChannelName), "PUT", new byte[] { });
            //    }
            //}            
            public void FollowChannel()
            {
                DateTime dt = DateTime.UtcNow;

                string payload = "{}";
                string stringifiedPayload = @"PUT%api.go-tellm.com%443%/api/v3/user/followChannel?channel=" + ChannelName + "%" + $"{dt:s}Z" + "%%" + payload;

                using (var client = new MyWebClient())
                {
                    client.Headers.Add(Constants.Header.ToHeader(stringifiedPayload, dt, true));
                    client.Encoding = Encoding.UTF8;
                    client.UploadString(Constants.LinkFollowChannel.ToLinkSecond(ChannelName), "PUT", payload);
                }
            }

            /// <summary>
            ///     Unfollows a channel.
            /// </summary>
            /// <param name="channel"></param>
            public void UnfollowChannel()
            {
                DateTime dt = DateTime.UtcNow;

                string payload = "{}";
                string stringifiedPayload = @"PUT%api.go-tellm.com%443%/api/v3/user/unfollowChannel?channel=" + ChannelName + "%" + $"{dt:s}Z" + "%%" + payload;

                using (var client = new MyWebClient())
                {
                    client.Headers.Add(Constants.Header.ToHeader(stringifiedPayload, dt, true));
                    client.Encoding = Encoding.UTF8;
                    client.UploadString(Constants.LinkUnfollowChannel.ToLinkSecond(ChannelName), "PUT", payload);
                }
            }

            /// <summary>
            ///     Get's all Jodels from this channel.
            /// </summary>
            /// <returns>List&lt;ChannelJodel&gt;.</returns>
            public List<ChannelJodel> GetJodels()
            {
                string plainJson;
                using (var client = new MyWebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    plainJson = client.DownloadString(Constants.LinkGetJodelsFromChannel.ToLinkSecond(ChannelName));
                }

                JsonJodels.RootObject myJodelsFromChannel = JsonConvert.DeserializeObject<JsonJodels.RootObject>(plainJson);
                return myJodelsFromChannel.recent.Select(item => new ChannelJodel(item)).ToList();
            }

            /// <summary>
            ///     Get's all Jodels from this channel.
            /// </summary>
            /// <param name="channel">The channel.</param>
            /// <returns>List&lt;ChannelJodel&gt;.</returns>
            public async Task<List<ChannelJodel>> GetJodelsAsync()
            {
                string plainJson;
                using (var client = new MyWebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    var taskResult = await Task.FromResult(
                        client.DownloadStringTaskAsync(new Uri(Constants.LinkGetJodelsFromChannel.ToLink(ChannelName))));
                    plainJson = taskResult.Result;
                }

                JsonJodels.RootObject myJodelsFromChannel = JsonConvert.DeserializeObject<JsonJodels.RootObject>(plainJson);
                return myJodelsFromChannel.recent.Select(item => new ChannelJodel(item)).ToList();
            }
        }
    }
}