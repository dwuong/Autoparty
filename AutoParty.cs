using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Interfaces;
using ImGuiNET;

namespace AutoParty
{
    public class AutoParty : BaseSettingsPlugin<AutoPartySettings>
    {
        private DateTime _nextInviteTime = DateTime.Now;
        private DateTime _nextPartyCheckTime = DateTime.Now;
        private int _inviteStep = 0;
        private DateTime _nextInviteStepTime = DateTime.Now;
        private string[] _parsedPlayersToInvite;
        private string _currentPlayerToInvite;
        private Queue<string> _playersToInviteQueue = new Queue<string>();

        public override bool Initialise()
        {
            Name = "Auto Party";
            ParsePlayerNames();
            return base.Initialise();
        }

        private void ParsePlayerNames()
        {
            _parsedPlayersToInvite = Settings.PlayersToInvite.Value
                                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(name => name.Trim())
                                            .Where(name => !string.IsNullOrEmpty(name))
                                            .ToArray();

            if (_parsedPlayersToInvite == null)
            {
                _parsedPlayersToInvite = new string[0];
            }
        }

        public override void DrawSettings()
        {
            base.DrawSettings();
            ImGui.Text("Current party members:");
            var partyList = GetCurrentPartyList();
            if (partyList != null && partyList.Any())
            {
                foreach (var member in partyList)
                {
                    ImGui.Text($"- {member}");
                }
            }
            else
            {
                ImGui.Text("No party members found.");
            }

            ImGui.Text($"Players in invite queue: {_playersToInviteQueue.Count}");
            foreach (var queuedPlayer in _playersToInviteQueue)
            {
                ImGui.Text($"- {queuedPlayer}");
            }
        }

        public override void Render()
        {
            if (!Settings.Enable.Value || GameController.IsLoading || !GameController.Window.IsForeground())
                return;

            if (DateTime.Now >= _nextPartyCheckTime)
            {
                _nextPartyCheckTime = DateTime.Now.AddSeconds(Settings.CheckInterval.Value);

                var currentPartyList = GetCurrentPartyList();
                if (currentPartyList == null) return;

                if (_inviteStep == 0) 
                {
                    _playersToInviteQueue.Clear();

                    if (_parsedPlayersToInvite != null && _parsedPlayersToInvite.Length > 0)
                    {
                        foreach (var playerToCheck in _parsedPlayersToInvite)
                        {
                            if (!string.IsNullOrEmpty(playerToCheck) && !currentPartyList.Any(p => p.Equals(playerToCheck, StringComparison.OrdinalIgnoreCase)))
                            {
                                _playersToInviteQueue.Enqueue(playerToCheck);
                            }
                        }
                    }
                }
            }

            if (_inviteStep == 0 && _playersToInviteQueue.Any() && DateTime.Now >= _nextInviteTime)
            {
                _currentPlayerToInvite = _playersToInviteQueue.Dequeue();
                _inviteStep = 1;
                _nextInviteStepTime = DateTime.Now;
            }

            if (_inviteStep != 0 && DateTime.Now >= _nextInviteStepTime)
            {
                switch (_inviteStep)
                {
                    case 1:
                        Keyboard.KeyPress(Keys.Enter);
                        _nextInviteStepTime = DateTime.Now.AddMilliseconds(50);
                        _inviteStep = 2;
                        break;

                    case 2:
                        if (_currentPlayerToInvite == null)
                        {
                            _inviteStep = 0;
                            return;
                        }
                        Keyboard.SendString($"/invite {_currentPlayerToInvite}");
                        _nextInviteStepTime = DateTime.Now.AddMilliseconds(50);
                        _inviteStep = 3;
                        break;

                    case 3:
                        Keyboard.KeyPress(Keys.Enter);
                        _nextInviteStepTime = DateTime.Now.AddMilliseconds(50);
                        
                        if (_playersToInviteQueue.Any())
                        {
                            _currentPlayerToInvite = _playersToInviteQueue.Dequeue();
                            _inviteStep = 1;
                        }
                        else
                        {
                            _nextInviteTime = DateTime.Now.AddMilliseconds(Settings.InviteCooldown.Value); 
                            _inviteStep = 0;
                            _currentPlayerToInvite = null;
                        }
                        break;

                    case 0:
                        break;

                    default:
                        _inviteStep = 0;
                        _currentPlayerToInvite = null;
                        break;
                }
            }
        }

        private List<string> GetCurrentPartyList()
        {
            var partyElements = new List<string>();
            try
            {
                var partyElementList = GameController?.IngameState?.IngameUi?.PartyElement?.Children?.ElementAtOrDefault(0)?.Children?.ElementAtOrDefault(0)?.Children;
                if (partyElementList == null) 
                {
                    return partyElements;
                }

                foreach (var partyElement in partyElementList)
                {
                    var playerName = partyElement?.Children?.ElementAtOrDefault(0)?.Text; 
                    if (!string.IsNullOrEmpty(playerName))
                    {
                        partyElements.Add(playerName);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"AutoParty: Error getting party list: {ex.Message}");
            }
            return partyElements;
        }
    }
}