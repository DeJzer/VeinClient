using VRC;
using UnityEngine;
using Il2CppSystem.Collections.Generic;
using PlagueButtonAPI.Misc;
using VRC.UI.Elements.Menus;
using System.Collections.Generic;
using System.Linq;

namespace VeinClient
{
    class Utils
    {
        /// <summary>
        /// Returns the local VRCPlayer.
        /// </summary>
        public static VRCPlayer GetLocalPlayer()
        {
            return VRCPlayer.field_Internal_Static_VRCPlayer_0;
        }

        public static Il2CppSystem.Collections.Generic.List<Player> GetAllPlayers()
        {
            // Make sure the PlayerManager exists first.
            if (PlayerManager.field_Private_Static_PlayerManager_0 == null)
            {
                return null;
            }

            return PlayerManager.field_Private_Static_PlayerManager_0?.field_Private_List_1_Player_0;
        }

        internal static Player GetCurrentlySelectedPlayer()
        {
            if (GameObject.Find("UserInterface").GetComponentInChildren<SelectedUserMenuQM>() == null)
            {
                return null;
            }

            return GetPlayerFromIDInLobby(GameObject.Find("UserInterface").gameObject.GetComponentInChildren<SelectedUserMenuQM>().field_Private_IUser_0.prop_String_0);
        }

        internal static Player GetPlayerFromIDInLobby(string id)
        {
            Il2CppSystem.Collections.Generic.List<Player> all_player = GetAllPlayers();

            foreach (var player in all_player)
            {
                if (player != null && player.prop_APIUser_0 != null)
                {
                    if (player.prop_APIUser_0.id == id)
                    {
                        return player;
                    }
                }
            }

            return null;
        }

        internal static Sprite CreateSpriteFromTex(Texture2D tex)
        {
            Sprite sprite = Sprite.CreateSprite(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f, 0, 0, new Vector4(), false);

            sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            return sprite;
        }

        private static GameObject AvatarPicTaker;
        internal static Texture2D TakePictureOfPlayer(VRCPlayer player)
        {
            var avatar = player.transform.Find("ForwardDirection/Avatar").gameObject;
            var manager = player.prop_VRCAvatarManager_0;

            var OldLayer = player == VRCPlayer.field_Internal_Static_VRCPlayer_0
                        ? (1 << LayerMask.NameToLayer("PlayerLocal"))
                        : (1 << LayerMask.NameToLayer("Player"));

            var layer = new System.Random().Next(0, 9999);

            avatar.layer = layer;

            if (AvatarPicTaker == null)
            {
                AvatarPicTaker = new GameObject("AvatarPicTaker");
            }

            AvatarPicTaker.SetActive(false);

            var CamComp = AvatarPicTaker.GetOrAddComponent<Camera>();

            CamComp.clearFlags = CameraClearFlags.SolidColor;
            CamComp.backgroundColor = new Color(0f, 0f, 0f, 0f);

            /* Enable camera */
            AvatarPicTaker.SetActive(true);

            /* Move camera infront of head */
            var descriptor = (manager.prop_VRCAvatarDescriptor_0 ?? manager.prop_VRC_AvatarDescriptor_1 ?? manager.prop_VRC_AvatarDescriptor_0);
            var head_height = descriptor.ViewPosition.y;
            var head = avatar.transform.position + new Vector3(0, head_height, 0);
            var target = head + avatar.transform.forward * 0.3f;
            var camera = CamComp;

            camera.useOcclusionCulling = false;
            camera.farClipPlane = 0.6f;
            camera.nearClipPlane = 0.05f;
            camera.transform.position = target;
            camera.transform.LookAt(head);

            camera.cullingMask = layer;
            camera.orthographic = true;
            camera.orthographicSize = head_height / 8;

            if (camera.targetTexture == null)
            {
                camera.targetTexture = new RenderTexture(256, 256, 0);
            }

            var currentRT = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;

            camera.Render();

            var image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height,
                    TextureFormat.RGBA32, false, true)
            { name = $"{player.field_Private_ApiAvatar_0.id}" };
            image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            image.Apply();
            image.hideFlags = HideFlags.DontUnloadUnusedAsset;

            RenderTexture.active = currentRT;

            AvatarPicTaker.SetActive(false);

            avatar.layer = OldLayer;

            return image;
        }

        /// <summary>
        /// Returns a list of active players.
        /// </summary

        /// <summary>
        /// Get a player object out of the specified player id.
        /// </summary>
        /// <param name="playerId">The player id to fetch.</param>
        public static Player GetPlayer(string playerId)
        {
            var players = GetAllPlayers();

            foreach (Player player in players)
            {
                // Make sure the player is valid first.
                if (player == null)
                {
                    continue;
                }

                if (player.field_Private_APIUser_0.id == playerId)
                {
                    return player;
                }
            }

            return null;
        }

        public static void ToggleOutline(Renderer renderer, bool state)
        {
            if (HighlightsFX.prop_HighlightsFX_0 == null)
            {
                return;
            }

            HighlightsFX.prop_HighlightsFX_0.Method_Public_Void_Renderer_Boolean_0(renderer, state);
        }

        [System.Serializable]
        public struct HSBColor
        {
            public float h;
            public float s;
            public float b;
            public float a;

            public HSBColor(float h, float s, float b, float a)
            {
                this.h = h;
                this.s = s;
                this.b = b;
                this.a = a;
            }

            public HSBColor(float h, float s, float b)
            {
                this.h = h;
                this.s = s;
                this.b = b;
                this.a = 1f;
            }

            public HSBColor(Color col)
            {
                HSBColor temp = FromColor(col);
                h = temp.h;
                s = temp.s;
                b = temp.b;
                a = temp.a;
            }

            public static HSBColor FromColor(Color color)
            {
                HSBColor ret = new HSBColor(0f, 0f, 0f, color.a);

                float r = color.r;
                float g = color.g;
                float b = color.b;

                float max = Mathf.Max(r, Mathf.Max(g, b));

                if (max <= 0)
                {
                    return ret;
                }

                float min = Mathf.Min(r, Mathf.Min(g, b));
                float dif = max - min;

                if (max > min)
                {
                    if (g == max)
                    {
                        ret.h = (b - r) / dif * 60f + 120f;
                    }
                    else if (b == max)
                    {
                        ret.h = (r - g) / dif * 60f + 240f;
                    }
                    else if (b > g)
                    {
                        ret.h = (g - b) / dif * 60f + 360f;
                    }
                    else
                    {
                        ret.h = (g - b) / dif * 60f;
                    }
                    if (ret.h < 0)
                    {
                        ret.h = ret.h + 360f;
                    }
                }
                else
                {
                    ret.h = 0;
                }

                ret.h *= 1f / 360f;
                ret.s = (dif / max) * 1f;
                ret.b = max;

                return ret;
            }

            public static Color ToColor(HSBColor hsbColor)
            {
                float r = hsbColor.b;
                float g = hsbColor.b;
                float b = hsbColor.b;

                if (hsbColor.s != 0)
                {
                    float max = hsbColor.b;
                    float dif = hsbColor.b * hsbColor.s;
                    float min = hsbColor.b - dif;

                    float h = hsbColor.h * 360f;

                    if (h < 60f)
                    {
                        r = max;
                        g = h * dif / 60f + min;
                        b = min;
                    }
                    else if (h < 120f)
                    {
                        r = -(h - 120f) * dif / 60f + min;
                        g = max;
                        b = min;
                    }
                    else if (h < 180f)
                    {
                        r = min;
                        g = max;
                        b = (h - 120f) * dif / 60f + min;
                    }
                    else if (h < 240f)
                    {
                        r = min;
                        g = -(h - 240f) * dif / 60f + min;
                        b = max;
                    }
                    else if (h < 300f)
                    {
                        r = (h - 240f) * dif / 60f + min;
                        g = min;
                        b = max;
                    }
                    else if (h <= 360f)
                    {
                        r = max;
                        g = min;
                        b = -(h - 360f) * dif / 60 + min;
                    }
                    else
                    {
                        r = 0;
                        g = 0;
                        b = 0;
                    }
                }

                return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), hsbColor.a);
            }

            public Color ToColor()
            {
                return ToColor(this);
            }

            public override string ToString()
            {
                return "H:" + h + " S:" + s + " B:" + b;
            }

            public static HSBColor Lerp(HSBColor a, HSBColor b, float t)
            {
                float h, s;

                // check special case black (color.b == 0): interpolate neither hue nor saturation!
                // check special case grey (color.s == 0): don't interpolate hue!
                if (a.b == 0)
                {
                    h = b.h;
                    s = b.s;
                }
                else if (b.b == 0)
                {
                    h = a.h;
                    s = a.s;
                }
                else
                {
                    if (a.s == 0)
                    {
                        h = b.h;
                    }
                    else if (b.s == 0)
                    {
                        h = a.h;
                    }
                    else
                    {
                        // works around bug with LerpAngle
                        float angle = Mathf.LerpAngle(a.h * 360f, b.h * 360f, t);
                       
                        while (angle < 0f)
                        {
                            angle += 360f;
                        }
                        
                        while (angle > 360f)
                        {
                            angle -= 360f;
                        }

                        h = angle / 360f;
                    }

                    s = Mathf.Lerp(a.s, b.s, t);
                }

                return new HSBColor(h, s, Mathf.Lerp(a.b, b.b, t), Mathf.Lerp(a.a, b.a, t));
            }
        }
    }
}
