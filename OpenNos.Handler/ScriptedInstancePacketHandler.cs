﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenNos.Core;
using OpenNos.GameObject;
using OpenNos.GameObject.Packets.ServerPackets;
using OpenNos.GameObject.Helpers;
using CloneExtensions;

namespace OpenNos.Handler
{
    class ScriptedInstancePacketHandler : IPacketHandler
    {
        #region Instantiation

        public ScriptedInstancePacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        private ClientSession Session { get; }

        /// <summary>
        /// treq packet
        /// </summary>
        /// <param name="treqPacket"></param>
        public void GetTreq(TreqPacket treqPacket)
        {

            ScriptedInstance timespace = Session.CurrentMapInstance.TimeSpaces.FirstOrDefault(s => treqPacket.X == s.PositionX && treqPacket.Y == s.PositionY);

            if (timespace != null)
            {
                if (treqPacket.StartPress == 1 || treqPacket.RecordPress == 1)
                {
                    timespace.LoadScript();
                    if (timespace.FirstMap == null) return;
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(Session, timespace.FirstMap.MapInstanceId);
                    timespace.FirstMap.InstanceBag.Creator = Session.Character.CharacterId;
                    Session.SendPackets(timespace.GenerateMinimap());
                }
                else
                {
                    Session.SendPacket(timespace.GenerateRbr());
                }
            }
        }

        /// <summary>
        /// GitPacket packet
        /// </summary>
        /// <param name="packet"></param>
        public void Git(GitPacket packet)
        {
            MapButton button = Session.CurrentMapInstance.Buttons.FirstOrDefault(s => s.MapButtonId == packet.ButtonId);
            if (button != null)
            {
                Session.CurrentMapInstance.Broadcast(button.GenerateOut());
                button.RunAction();
                Session.CurrentMapInstance.Broadcast(button.GenerateIn());
            }
        }
        /// <summary>
        /// wreq packet
        /// </summary>
        /// <param name="packet"></param>
        public void GetWreq(WreqPacket packet)
        {
            foreach (ScriptedInstance portal in Session.CurrentMapInstance.TimeSpaces)
            {
                if (Session.Character.PositionY >= portal.PositionY - 1 && Session.Character.PositionY <= portal.PositionY + 1
                    && Session.Character.PositionX >= portal.PositionX - 1 && Session.Character.PositionX <= portal.PositionX + 1)
                {
                    switch (packet.Value)
                    {
                        case 0:
                            if (Session.Character.Group != null && Session.Character.Group.Characters.Any(s => !s.CurrentMapInstance.InstanceBag.Lock && s.Character.MapX == portal.PositionX && s.Character.MapY == portal.PositionY))
                            {
                                UserInterfaceHelper.Instance.GenerateDialog($"#wreq^3^{Session.Character.CharacterId} #wreq^0^1 {Language.Instance.GetMessageFromKey("ASK_JOIN_TEAM_TS")}");
                            }
                            else
                            {
                                Session.SendPacket(portal.GenerateRbr());
                            }
                            break;

                        case 3:
                            if (Session.Character.Group != null)
                            {
                                ClientSession character = Session.Character.Group.Characters.Where(s => s.Character.CharacterId == packet.Param).FirstOrDefault();
                                if (character != null)
                                {
                                    MapCell mapcell = character.CurrentMapInstance.Map.GetRandomPosition();
                                    Session.Character.MapX = portal.PositionX;
                                    Session.Character.MapY = portal.PositionY;
                                    ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, character.CurrentMapInstance.MapInstanceId, mapcell.X, mapcell.Y);
                                }
                            }
                            break;
                    }
                }
            }
        }


        #endregion
    }
}
