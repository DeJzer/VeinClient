using HarmonyLib;
using LoadSprite;
using VRC;
using VRC.Core;
using VRC.Animation; 
using UnityEngine;
using MelonLoader;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine.UI;
using VRC.UI.Core.Styles;
using UIExpansionKit.API;
using VRC.UI.Elements;
using PlagueButtonAPI;
using PlagueButtonAPI.Misc;
using PlagueButtonAPI.Controls;
using PlagueButtonAPI.Controls.Grouping;
using PlagueButtonAPI.Pages;
using Random = System.Random;

namespace VeinClient 
{
    public class VeinMain : MelonMod
    {
        internal static Sprite LoadedImage = null;
        internal static bool DisablePortals = false;
        internal static Sprite ButtonImage = null;
        #region spoof static
        private static MelonPreferences_Category PreferencesCategory;
        private static MelonPreferences_Entry<float> PreferenceFPS, PreferenceFPSVariance;
        private static MelonPreferences_Entry<int> PreferencePing, PreferencePingVariance, PreferencVarianceMin, PreferencVarianceMax;

        // A value to be added to the configured FPS spoof value
        private static float VarianceFPS = 0f;
        // A value to be added to the configured ping spoof value
        private static int VariancePing = 0;
        // Time for when next variance update can be run after.
        private System.DateTime _next_variance_update_after = System.DateTime.Now;

        private const string PreferencesIdentifier = "NoDetailsForClienters";
        #endregion

        public override void OnApplicationStart()
        {
            if (File.Exists(Environment.CurrentDirectory + "\\ImageToLoad.png"))
            {
                LoadedImage = (Environment.CurrentDirectory + "\\ImageToLoad.png").LoadSpriteFromDisk();
            };
            MelonLogger.Msg("OnApplicationStart().");

            MelonLogger.Msg("Hooking NetworkManager.");
            Hooks.NetworkManagerHook.Initialize();
            Hooks.NetworkManagerHook.OnJoin += OnPlayerJoined;
            Hooks.NetworkManagerHook.OnLeave += OnPlayerLeft;

            Drawing.CreateLineMaterial();
            #region SpoofAppStart
            // Preferences setup

            var patchingSuccess = true;

            try // Patch `UnityEngine.Time.smoothDeltaTime` to use our Harmony PatchFPS Prefix
            {
                HarmonyInstance.Patch(
                    typeof(Time).GetProperty("smoothDeltaTime").GetGetMethod(),
                    prefix: new HarmonyMethod(typeof(VeinMain).GetMethod("PatchFPS", BindingFlags.NonPublic | BindingFlags.Static))
                );
            }
            catch (System.Exception ex)
            {
                patchingSuccess = false;
                MelonLogger.Error($"Failed to patch FPS: {ex}");
            }

            try 
            {
                HarmonyInstance.Patch(
                    typeof(ExitGames.Client.Photon.PhotonPeer).GetProperty("RoundTripTime").GetGetMethod(),
                    prefix: new HarmonyMethod(typeof(VeinMain).GetMethod("PatchPing", BindingFlags.NonPublic | BindingFlags.Static))
                );
            }
            catch (System.Exception ex)
            {
                patchingSuccess = false;
                MelonLogger.Error($"Failed to patch ping: {ex}");
            }

            if (patchingSuccess) MelonLogger.Msg("Applied successfully.");
            #endregion
        }

        private Dictionary<string, Sprite> UserImages = new Dictionary<string, Sprite>();
        private void NetworkEvents_OnAvatarInstantiated(VRCAvatarManager arg1, VRC.Core.ApiAvatar arg2, GameObject arg3)
        {
            var tex = Utils.TakePictureOfPlayer(arg1.field_Private_VRCPlayer_0);

            var sprite = Utils.CreateSpriteFromTex(tex);

            UserImages[arg1.field_Private_VRCPlayer_0.gameObject.GetOrAddComponent<Player>().field_Private_APIUser_0.id] = sprite;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "ui")
            {
                ButtonAPI.OnInit += () =>
                {
                    var Page = new MenuPage("TestMenu_1", "Vein Client");

                    new Tab(Page, "Menu Client", ButtonImage);

                    var FunctionalGroup = new ButtonGroup(Page, "Malicious Functions");

                    //World hacks button
                    new SingleButton(FunctionalGroup, "World Hacks", "Opens menu to hack worlds", () =>
                    {

                    }, true, ButtonImage);

                    #region MovMenu
                    var MovementMenu = new MenuPage("Movement_1", "Movements Menu", false);

                    var MovGroup = new ButtonGroup(MovementMenu, "", true, TextAnchor.UpperLeft);
                    var Handler2 = MovGroup.gameObject.GetOrAddComponent<ObjectHandler>();

                    Handler2.OnUpdateEachSecond += (obj, IsEnabled) =>
                    {
                        if (IsEnabled)
                        {
                            MovGroup.gameObject.transform.DestroyChildren();

                            //Fly button
                            new ToggleButton(MovGroup, "Toggle FLY", "Turn ON fly", "Turn OFF fly", (val) =>
                            {
                                if (val)
                                {
                                    Features.Noclip.noclipEnabled = true;
                                }
                                else
                                {
                                    Features.Noclip.noclipEnabled = false;

                                    VRCPlayer localPlayer = Utils.GetLocalPlayer();
                                    localPlayer.GetComponent<VRCMotionState>().field_Private_CharacterController_0.enabled = true;
                                }
                            }).SetToggleState(false, true);

                            //Speedhack speed
                            new PlagueButtonAPI.Controls.Slider(MovGroup, "Speed FLY\n", "Changing speed of fly under shift", (val) =>
                            {
                                // changing value
                                Features.Speedhack.speedMultiplier = val;
                            }); ;

                            //Anti-portal button
                            new ToggleButton(MovGroup, "Destroys portals", "Turn ON Anti-Portal", "Turn OFF Anti-Portal", (val) =>
                            {
                                if (val)
                                {
                                    Features.AntiPortal.antiPortalEnabled = true;
                                }
                                else
                                {
                                    Features.AntiPortal.antiPortalEnabled = false;
                                }
                            }).SetToggleState(false, true);

                        }
                    };

                    //MovementsMenu options
                    new SingleButton(FunctionalGroup, "Movements", "Opens menu with movements things", () =>
                    {
                        MovementMenu.OpenMenu();
                    }, true, ButtonImage);
                    #endregion

                    //SpooferMenu
                    #region SpooferMenu
                    var SpooferMenu = new MenuPage("Spoofers_1", "Spoofers", false);

                    var SpooferGroup = new ButtonGroup(SpooferMenu, "", true, TextAnchor.UpperLeft);
                    var Handler1 = SpooferGroup.gameObject.GetOrAddComponent<ObjectHandler>();

                    Handler1.OnUpdateEachSecond += (obj, IsEnabled) =>
                    {
                        if (IsEnabled)
                        {
                            SpooferGroup.gameObject.transform.DestroyChildren();

                            //Ping Spoof button
                            new ToggleButton(SpooferGroup, "Ping Spoof", "Turn ON ping spoofer", "Turn OFF ping spoofer", (val) =>
                            {
                                VeinMain.VarianceFPS = 0;
                            }).SetToggleState(false, true);

                            //FPS Spoof button
                            new ToggleButton(SpooferGroup, "FPS Spoof", "Turn ON FPS spoofer", "Turn OFF FPS spoofer", (val) =>
                            {

                            }).SetToggleState(false, true);

                            //FPS spoof slider
                            new PlagueButtonAPI.Controls.Slider(SpooferGroup, "FPS number", "Changing number of FPS", (val) =>
                            {
                                VeinMain.VarianceFPS = val;
                            });
                        }
                    };

                    //SpooferMenu options
                    new SingleButton(FunctionalGroup, "Spoofer", "Opens menu with spoof things", () =>
                    {
                        SpooferMenu.OpenMenu();
                    }, true, ButtonImage);
                    #endregion

                    //PlayerListMenu
                    #region PlayerListMenu
                    var PlayerListMenu = new MenuPage("PlayersList_1", "Player List", false);

                    var PlayersGroup = new ButtonGroup(PlayerListMenu, "", true, TextAnchor.UpperLeft);
                    var Handler = PlayersGroup.gameObject.GetOrAddComponent<ObjectHandler>();

                    Handler.OnUpdateEachSecond += (obj, IsEnabled) =>
                    {
                        if (IsEnabled)
                        {
                            PlayersGroup.gameObject.transform.DestroyChildren();

                            foreach (var player in Utils.GetAllPlayers())
                            {
                                var image = UserImages.ContainsKey(player.field_Private_APIUser_0.id) ? UserImages[player.field_Private_APIUser_0.id] : null;

                                if (player.field_Private_APIUser_0 == null)
                                {
                                    MelonLogger.Error("Null APIUser!");
                                    continue;
                                }

                                new SingleButton(PlayersGroup, player.field_Private_APIUser_0.displayName, "Selects This Player", () =>
                                {

                                }, true, image);
                            }
                        }
                    };

                    new SingleButton(FunctionalGroup, "Player List", "Opens A Basic Player List", () =>
                    {
                        PlayerListMenu.OpenMenu();
                    }, true, ButtonImage);
                    #endregion

                    var NonFunctionalGroup = new ButtonGroup(Page, "Informations");

                    //SettingsMenu
                    #region SettingsMenu
                    var SettingsMenu = new MenuPage("Settings_1", "Settings", false);

                    var SettingsGroup = new ButtonGroup(SettingsMenu, "", true, TextAnchor.UpperLeft);
                    var Handler3 = SettingsGroup.gameObject.GetOrAddComponent<ObjectHandler>();

                    Handler3.OnUpdateEachSecond += (obj, IsEnabled) =>
                    {
                        if (IsEnabled)
                        {
                            SettingsGroup.gameObject.transform.DestroyChildren();

                        }
                    };

                    new SingleButton(NonFunctionalGroup, "Settings", "Opens settings for client", () =>
                    {
                        SettingsMenu.OpenMenu();
                    }, true, ButtonImage);
                    #endregion

                    //DiscordButton
                    #region DiscordButton
                    new SingleButton(NonFunctionalGroup, "Discord", "Opens link to discord", () =>
                    {

                    }, true, ButtonImage);
                    #endregion

                    //CreditsMenu
                    #region CreditsMenu
                    var CreditsMenu = new MenuPage("Credits_1", "Credits", false);

                    var CreditsGroup = new ButtonGroup(CreditsMenu, "", true, TextAnchor.UpperLeft);
                    var Handler4 = CreditsGroup.gameObject.GetOrAddComponent<ObjectHandler>();

                    Handler4.OnUpdateEachSecond += (obj, IsEnabled) =>
                    {
                        if (IsEnabled)
                        {
                            CreditsGroup.gameObject.transform.DestroyChildren();

                        }
                    };

                    new SingleButton(NonFunctionalGroup, "Credits", "Opens credits of client", () =>
                    {
                        CreditsMenu.OpenMenu();
                    }, true, ButtonImage);
                    #endregion

                    //code for dropdown
                    #region dropdownMenu
                    /*                    var Dropdown = new CollapsibleButtonGroup(Page, "Xionek chuj <3", "Toggles The Dropdown");

                                        new SingleButton(Dropdown, "Button", "Button", () =>
                                        {
                                            MelonLogger.Msg("Button Clicked!"); 
                                        }, false, ButtonImage);

                                        new Label(Dropdown, "Label", "Label", () =>
                                        {
                                            MelonLogger.Msg("Label Clicked!");
                                        });*/
                    #endregion

                    new Label(Page, "Vein sends regards :)", null);

                    //Ideally Use UIX For Menu Entering Instead Of Tabs. You Can Also Use UIX When You Select Someone To Enter Your Menu.
                    Player SelectedPlayer = null;

                    var UserPage = new MenuPage("UserTestMenu_1", "User Menu");

                    var OptionsGroup = new ButtonGroup(UserPage, "Options");

                    new SingleButton(OptionsGroup, "Button", "Button", () =>
                    {
                        MelonLogger.Msg("Button Clicked! - Selected Player: " + (SelectedPlayer != null ? SelectedPlayer.field_Private_APIUser_0.displayName : "<Null>"));
                    }, false, ButtonImage);

                    ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserQuickMenu).AddSimpleButton("User Options Example", () =>
                    {
                        SelectedPlayer = Utils.GetCurrentlySelectedPlayer();

                        UserPage.SetTitle("User Menu: " + (SelectedPlayer != null ? SelectedPlayer.field_Private_APIUser_0.displayName : "<Null>"));

                        UserPage.OpenMenu();
                    });

                    ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserQuickMenuRemote).AddSimpleButton("User Options Example", () =>
                    {
                        SelectedPlayer = Utils.GetCurrentlySelectedPlayer();

                        UserPage.SetTitle("User Menu: " + (SelectedPlayer != null ? SelectedPlayer.field_Private_APIUser_0.displayName : "<Null>"));

                        UserPage.OpenMenu();
                    });
                };
            }
        }

        internal static float OnUpdateRoutineDelay = 0f;

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"OnSceneWasInitialized({buildIndex}, \"{sceneName}\")");

            Features.Worlds.JustBClub.Initialize(sceneName);
            Features.Worlds.AmongUs.Initialize(sceneName);

            base.OnSceneWasInitialized(buildIndex, sceneName);
        }

        #region SpooferFPSPublicVoid
        public override void OnFixedUpdate()
        {
            // Only run variance every so often so that it doesn't jitter so much in an obvious way.
            if (_next_variance_update_after < System.DateTime.Now)
            {
                var rng = new System.Random();
                _next_variance_update_after =
                    System.DateTime.Now.AddMilliseconds(rng.Next(PreferencVarianceMin.Value, PreferencVarianceMax.Value));

                if (PreferenceFPSVariance.Value <= 0) VarianceFPS = 0f;
                else VarianceFPS = (PreferenceFPSVariance.Value) * (float)rng.NextDouble();

                if (PreferencePingVariance.Value <= 0) VariancePing = 0;
                else VariancePing = rng.Next(0, PreferencePingVariance.Value);
            }
        }
        #endregion

        #region PatchForSpoofFPS
        // The patch for spoofing FPS
        private static bool PatchFPS(ref float __result)
        {
            // Run original getter if spoofing is disabled
            if (PreferenceFPS.Value < 0) return true;
            // Otherwise use our value and don't run original getter.
            __result = 1f / (PreferenceFPS.Value + VarianceFPS);
            return false;
        }

        // The patch for spoofing ping
        private static bool PatchPing(ref int __result)
        {
            // Run original getter if spoofing is disabled
            if (PreferencePing.Value < 0) return true;
            // Otherwise use our value and don't run original getter.
            __result = PreferencePing.Value + VariancePing;
            return false;
        }
        #endregion
        public override void OnUpdate()
        {
            try
            {
                if (VRCUtils.IsWorldLoaded && Time.time > OnUpdateRoutineDelay)
                {
                    OnUpdateRoutineDelay = Time.time + 1f;
                    if (DisablePortals)
                    {
                        Functions.TogglePortals(false);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Msg("Error in OnUpdate! - " + ex.Message + " From: " + ex.Source + " - Stack: " + ex.StackTrace);
            }

            // Toggling our menu.
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                Menu.ToggleMenu();
            }

            // Speedhack.
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyUp(KeyCode.X))
            {
                Features.Speedhack.Toggle();
            }

            // Failsafe for when the game lags while letting go of X preventing speedhack to turn off.
            if (!Input.GetKey(KeyCode.X) && Features.Speedhack.speedEnabled)
            {
                Features.Speedhack.speedEnabled = false;
            }

            // Noclip.
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F))
            {
                Features.Noclip.Toggle();
            }

            Features.ESP.UpdateColors();
            Features.ESP.Main();
            Features.Noclip.Main();
            Features.Speedhack.Main();
            Features.AntiPortal.Main();
            Features.Worlds.AmongUs.OnUpdate();
        }

        public override void OnGUI()
        {
            // Handle menu rendering.
            Menu.Main();

            // Draw text for ESP.
            Features.ESP.UserInformationESP();

            // Draw line ESP.
            Features.ESP.LineESP();

            // Handle cursor locking to allow interaction with our menu.
            Menu.HandleCursor();

            base.OnGUI();
        }

        public void OnPlayerJoined(Player player)
		{
            APIUser apiUser = player.prop_APIUser_0;

            if (apiUser == null)
            {
                return;
            }

            MelonLogger.Msg($"Player \"{apiUser.displayName}\" joined.");
        }

        public void OnPlayerLeft(Player player)
        {
            APIUser apiUser = player.prop_APIUser_0;

            if (apiUser == null)
            {
                return;
            }

            MelonLogger.Msg($"Player \"{apiUser.displayName}\" left.");
        }
    }
}
