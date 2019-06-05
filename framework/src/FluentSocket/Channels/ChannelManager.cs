using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using FluentSocket.Extensions;
using FluentSocket.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace FluentSocket.Channels
{
    public class ChannelManager : IChannelManager
    {
        private object SyncObject = new object();
        private readonly ILogger _logger;
        public IChannelGroup Group { get; set; }
        private List<ChannelInfo> Channels { get; set; } = new List<ChannelInfo>();
        public ChannelManager(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(FluentSocketSettings.LoggerName);
        }

        public void Initialize(IChannelGroup channelGroup)
        {
            lock (SyncObject)
            {
                if (Group == null)
                {
                    Group = channelGroup;
                }
            }
        }

        public ChannelInfo AddChannel(IChannel channel)
        {
            lock (SyncObject)
            {
                Group.Add(channel);

                var channelInfo = new ChannelInfo(channel.Id.AsLongText(),
                    ((IPEndPoint)channel.RemoteAddress).ToStringAddress(),
                    ObjectId.GenerateNewStringId());
                Channels.Add(channelInfo);
                _logger.LogInformation($"{nameof(ChannelManager)},Add new channel:{channelInfo.ToString()}");
                return channelInfo;
            }
        }

        public ChannelInfo RemoveChannel(Func<ChannelInfo, bool> predicate)
        {
            lock (SyncObject)
            {
                var channelInfo = Channels.FirstOrDefault(predicate);
                Channels.Remove(channelInfo);
                Group.FirstOrDefault(x => x.Id.AsLongText() == channelInfo.ChannelId);
                _logger.LogInformation($"{nameof(ChannelManager)},Remove channel:{channelInfo.ToString()}");
                return channelInfo;
            }
        }

        public ChannelInfo FindChannelInfo(Func<ChannelInfo, bool> predicate)
        {
            return Channels.FirstOrDefault(predicate);
        }


        public IChannel FindFirstChannel(Func<ChannelInfo, bool> predicate)
        {
            var channelInfo = Channels.FirstOrDefault(predicate);
            if (channelInfo != null)
            {
                return Group.FirstOrDefault(x => x.Id.AsLongText() == channelInfo.ChannelId);
            }
            return null;
        }

        public List<IChannel> FindChannels(Func<ChannelInfo, bool> predicate)
        {
            var channelShortIds = Channels.Where(predicate).Select(x => x.ChannelId);
            return Group.Where(x => channelShortIds.Contains(x.Id.AsLongText())).ToList();
        }


    }
}
