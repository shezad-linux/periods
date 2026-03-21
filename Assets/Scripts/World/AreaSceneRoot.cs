using System.Collections.Generic;
using Lumenfall.Core;
using Lumenfall.Data;
using Lumenfall.Services;
using Lumenfall.World.Rooms;
using UnityEngine;

namespace Lumenfall.World
{
    public sealed class AreaSceneRoot : MonoBehaviour
    {
        [SerializeField] private AreaDefinition areaDefinition;
        [SerializeField] private List<RoomRoot> rooms = new();

        private RoomRoot _activeRoom;

        public AreaDefinition AreaDefinition => areaDefinition;

        public void Initialize(AreaDefinition definition)
        {
            areaDefinition = definition;
        }

        private void Awake()
        {
            if (rooms.Count == 0)
            {
                rooms.AddRange(GetComponentsInChildren<RoomRoot>(true));
            }

            foreach (RoomRoot room in rooms)
            {
                room.BindToArea(this);
                room.SetRoomActive(false);
            }
        }

        private void Start()
        {
            if (areaDefinition != null && ServiceRegistry.TryGet(out GameStateService gameStateService))
            {
                gameStateService.RegisterAreaLoaded(areaDefinition.sceneName);
            }

            if (rooms.Count > 0)
            {
                EnterRoom(rooms[0]);
            }
        }

        public void RegisterRoom(RoomRoot roomRoot)
        {
            if (roomRoot != null && !rooms.Contains(roomRoot))
            {
                rooms.Add(roomRoot);
            }
        }

        public void EnterRoom(RoomRoot roomRoot)
        {
            if (roomRoot == null || roomRoot == _activeRoom)
            {
                return;
            }

            if (_activeRoom != null)
            {
                _activeRoom.SetRoomActive(false);
            }

            _activeRoom = roomRoot;
            _activeRoom.SetRoomActive(true);

            if (ServiceRegistry.TryGet(out GameStateService gameStateService))
            {
                gameStateService.SetActiveRoom(roomRoot.Definition);
            }
        }
    }
}
