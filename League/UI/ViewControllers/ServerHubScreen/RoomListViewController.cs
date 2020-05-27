﻿using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using VRrhythmLeague.Data;
using VRrhythmLeague.UI.FlowCoordinators;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRrhythmLeague.UI.ViewControllers.ServerHubScreen
{
    class RoomListViewController : BSMLResourceViewController
    {
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        public event Action createRoomButtonPressed;
        public event Action<ServerHubRoom, string> selectedRoom;
        public event Action refreshPressed;
        [UIParams]
        private BSMLParserParams parserParams;

        [UIComponent("refresh-btn")]
        private Button _refreshButton;

        [UIComponent("rooms-list")]
        public CustomCellListTableData roomsList;

        [UIComponent("password-keyboard")]
        ModalKeyboard _passwordKeyboard;

        [UIComponent("hubs-text")]
        TextMeshProUGUI _hubsCountText;
        [UIComponent("no-rooms-text")]
        TextMeshProUGUI _noRoomsText;

        [UIValue("rooms")]
        public List<object> roomInfosList = new List<object>();

        private ServerHubRoom _selectedRoom;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            if (firstActivation)
            {
                _hubsCountText.gameObject.AddComponent<LayoutElement>().preferredWidth = 35f;
            }

            roomsList.tableView.ClearSelection();
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            parserParams.EmitEvent("closeAllMPModals");
            base.DidDeactivate(deactivationType);
        }

        public void SetRooms(List<ServerHubRoom> rooms)
        {
            roomInfosList.Clear();

            if (rooms != null)
            {
                var availableRooms = rooms.OrderByDescending(y => y.roomInfo.players);

                foreach (ServerHubRoom room in availableRooms)
                {
                    roomInfosList.Add(new RoomListObject(room));
                }
            }

            if(rooms == null || rooms.Count == 0)
                _noRoomsText.enabled = true;
            else
                _noRoomsText.enabled = false;

            roomsList.tableView.ReloadData();

        }

        public void SetServerHubsCount(int online, int total)
        {
            _hubsCountText.text = $"  Hubs online: {online}/{total}";

        }

        public void SetRefreshButtonState(bool enabled)
        {
            _refreshButton.interactable = enabled;
        }

        [UIAction("room-selected")]
        private void RoomSelected(TableView sender, RoomListObject obj)
        {
            if (!obj.room.roomInfo.usePassword)
            {
                selectedRoom?.Invoke(obj.room, null);
            }
            else
            {
                _selectedRoom = obj.room;
                _passwordKeyboard.modalView.Show(true);
            }
        }

        [UIAction("join-pressed")]
        private void PasswordEntered(string pass)
        {
            selectedRoom?.Invoke(_selectedRoom, pass);
        }

        [UIAction("create-room-btn-pressed")]
        private void CreateRoomBtnPressed()
        {
            createRoomButtonPressed?.Invoke();
        }

        [UIAction("refresh-btn-pressed")]
        private void RefreshBtnPressed()
        {
            refreshPressed?.Invoke();
        }

        public class RoomListObject
        {
            public ServerHubRoom room;

            [UIValue("room-name")]
            private string roomName;

            [UIValue("room-state")]
            private string roomStateString;

            [UIComponent("locked-icon")]
            private RawImage lockedIcon;
            private bool locked;

            [UIComponent("bg")]
            private RawImage background;

            [UIComponent("room-state-text")]
            private TextMeshProUGUI roomStateText;

            public RoomListObject(ServerHubRoom room)
            {
                this.room = room;
                roomName = $"({room.roomInfo.players}/{((room.roomInfo.maxPlayers == 0) ? "INF" : room.roomInfo.maxPlayers.ToString())}) {room.roomInfo.name}";
                switch (room.roomInfo.roomState)
                {
                    case RoomState.InGame:
                        roomStateString = "In game";
                        break;
                    case RoomState.Preparing:
                        roomStateString = "Preparing";
                        break;
                    case RoomState.Results:
                        roomStateString = "Results";
                        break;
                    case RoomState.SelectingSong:
                        roomStateString = "Selecting song";
                        break;
                    default:
                        roomStateString = room.roomInfo.roomState.ToString();
                        break;
                }
                locked = room.roomInfo.usePassword;
            }

            [UIAction("refresh-visuals")]
            public void Refresh(bool selected, bool highlighted)
            {
                lockedIcon.texture = Sprites.lockedRoomIcon.texture;
                lockedIcon.enabled = locked;
                background.texture = Sprites.whitePixel.texture;
                background.color = new Color(1f, 1f, 1f, 0.125f);
                roomStateText.color = new Color(0.65f, 0.65f, 0.65f, 1f);
            }
        }
    }
}
