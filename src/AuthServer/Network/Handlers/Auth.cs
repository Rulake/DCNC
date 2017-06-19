﻿using System;
using System.Net;
using Shared;
using Shared.Models;
using Shared.Network;
using Shared.Network.AuthServer;
using Shared.Objects;
using Shared.Util;

namespace AuthServer.Network.Handlers
{
    public static class Authentication
    {
        [Packet(Packets.CmdServerMessage)]
        public static void ServerMessage(Packet packet)
        {
            // TODO: Send serverlist here
            var pkt = new Packet(23);
            UserAuthAnswerPacket.Server[] Servers = new UserAuthAnswerPacket.Server[1];
            Servers[0] = new UserAuthAnswerPacket.Server
            {
                ServerName = "Test",
                ServerId = 1,
                PlayerCount = 0.0f,
                MaxPlayers = 7000.0f,
                ServerState = 1,
                GameTime = Environment.TickCount,
                LobbyTime = Environment.TickCount,
                Area1Time = Environment.TickCount,
                Area2Time = Environment.TickCount,
                RankingUpdateTime = Environment.TickCount,
                GameServerIp = IPAddress.Parse("127.0.0.1").GetAddressBytes(),
                LobbyServerIp = IPAddress.Parse("127.0.0.1").GetAddressBytes(),
                AreaServer1Ip = IPAddress.Parse("127.0.0.1").GetAddressBytes(),
                AreaServer2Ip = IPAddress.Parse("127.0.0.1").GetAddressBytes(),
                RankingServerIp = IPAddress.Parse("127.0.0.1").GetAddressBytes(),
                GameServerPort = 11021,
                LobbyServerPort = 11011,
                AreaServerPort = 11031,
                AreaServer2Port = 11041,
                AreaServerUdpPort = 10701,
                AreaServer2UdpPort = 10702,
                RankingServerPort = 11078
            };

            pkt.Writer.Write(1); // Size
            //for int i = 0; i < size; i++
            for (int i = 0; i < Servers.Length; i++)
            {
                pkt.Writer.WriteUnicodeStatic(Servers[i].ServerName, 32); // 32
                pkt.Writer.Write(Servers[i].ServerId);
                pkt.Writer.Write(Servers[i].PlayerCount);
                pkt.Writer.Write(Servers[i].MaxPlayers);
                pkt.Writer.Write(Servers[i].ServerState);
                if (Servers[i].ServerState == 100)
                {
                    pkt.Writer.Write(0);
                    pkt.Writer.Write(0);
                    pkt.Writer.Write(0);
                    pkt.Writer.Write(0);
                }
                else
                {
                    pkt.Writer.Write(Servers[i].GameTime);
                    pkt.Writer.Write(Servers[i].LobbyTime);
                    pkt.Writer.Write(Servers[i].Area1Time);
                    pkt.Writer.Write(Servers[i].Area2Time);
                }
                pkt.Writer.Write(Servers[i].RankingUpdateTime);
                pkt.Writer.Write(Servers[i].GameServerIp);
                pkt.Writer.Write(Servers[i].LobbyServerIp);
                pkt.Writer.Write(Servers[i].AreaServer1Ip);
                pkt.Writer.Write(Servers[i].AreaServer2Ip);
                pkt.Writer.Write(Servers[i].RankingServerIp);
                pkt.Writer.Write(Servers[i].GameServerPort);
                pkt.Writer.Write(Servers[i].LobbyServerPort);
                pkt.Writer.Write(Servers[i].AreaServerPort);
                pkt.Writer.Write(Servers[i].AreaServer2Port);
                pkt.Writer.Write(Servers[i].AreaServerUdpPort);
                pkt.Writer.Write(Servers[i].AreaServer2UdpPort);
                pkt.Writer.Write(Servers[i].RankingServerPort);
            }
            packet.Sender.Send(pkt);

            var serverId = packet.Reader.ReadInt32();

            Packet ack = new Packet(Packets.CmdServerMessage + 1);
            if (serverId != 0)
            {
                ack.Writer.Write(serverId);
                ack.Writer.WriteUnicode("Hello world! This is a basic server message!");
            }
            else
            {
                ack.Writer.Write(serverId);
                ack.Writer.Write(0);
            }
            packet.Sender.Send(ack);
        }

        [Packet(Packets.CmdUserAuth)]
        public static void UserAuth(Packet packet)
        {
            UserAuthPacket authPacket = new UserAuthPacket(packet);

            Log.Debug("Login (v{0}) request from {1}", authPacket.ProtocolVersion.ToString(), authPacket.Username);

            if (authPacket.ProtocolVersion < ServerMain.ProtocolVersion)
            {
                Log.Debug("Client too old?");
                packet.Sender.Error("Your client is outdated!");
            }

            if (!AccountModel.AccountExists(AuthServer.Instance.Database.Connection, authPacket.Username))
            {
                Log.Debug("Account {0} not found!", authPacket.Username);
                packet.Sender.Error("Invalid Username or password!");
                return;
            }

            User user = AccountModel.Retrieve(AuthServer.Instance.Database.Connection, authPacket.Username);
            if (user == null)
            {
                Log.Debug("Account {0} not found!", authPacket.Username);
                packet.Sender.Error("Invalid Username or password!");
                return;
            }
            var passwordHashed = Password.GenerateSaltedHash(authPacket.Password, user.Salt);
            if(passwordHashed != user.Password)
            {
                Log.Debug("Account {0} found but invalid password! ({1} ({2}) vs {3})", authPacket.Username, passwordHashed, user.Salt, user.Password);
                packet.Sender.Error("Invalid Username or password!");
                return;
            }

            uint ticket = AccountModel.CreateSession(AuthServer.Instance.Database.Connection, authPacket.Username);

            // Wrong protocol -> 20070

            /*var ack = new Packet(Packets.UserAuthAck);
            packet.Sender.Error("Invalid Username or password!");*/

            var ack = new UserAuthAnswerPacket
            {
                Ticket = ticket,
                Servers = ServerModel.Retrieve(AuthServer.Instance.Database.Connection).ToArray()
            };

            ack.Send(Packets.UserAuthAck, packet.Sender);
        }
    }
}

