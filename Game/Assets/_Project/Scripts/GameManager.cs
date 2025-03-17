using System.Net;
using TMPro;
using UnityEngine;

namespace _Project.Scripts {
    public class GameManager : MonoBehaviour {
        public static Mode Mode { get; private set; }
        public static Vector3 Position;

        [SerializeField] CanvasGroup authenticationCanvas, authenticationInputCanvas, chatInputCanvas;
        [SerializeField] Client client;
        [SerializeField] TMP_InputField authenticationInputText;
        string address;

        void Awake() {
            Mode = Mode.Authentication;
        }

        void OnEnable() {
            Client.OnConnect += OnConnect;
        }

        void OnDisable() {
            Client.OnConnect -= OnConnect;
        }

        void Update() {
            if (Input.GetKeyDown(KeyCode.Tab)) {
                Mode = Mode == Mode.Move ? Mode.Chat : Mode.Move;
                SetMode(Mode);
            }
        }

        public void SetAddressText(string text) => address = text;
        public void SetLoopback() {
            address = IPAddress.Loopback.ToString();
            authenticationInputText.text = address;
        }
        public void Submit() {
            try {
                var ip = IPAddress.Parse(address);
                authenticationInputCanvas.alpha = 0;
                authenticationInputCanvas.interactable = false;
                authenticationInputCanvas.blocksRaycasts = false;
                client.Initialize(ip);
            } catch {
                authenticationInputText.text = "Invalid IP address";
            }
        }

        void SetMode(Mode mode) {
            Mode = mode;
            switch(mode) {
                case Mode.Move: {
                    OnSetMove();
                }
                    break;

                case Mode.Chat: {
                    OnSetChat();
                }
                    break;

                default: return;
            }
        }

        void OnConnect() {
            authenticationCanvas.alpha = 0;
            authenticationCanvas.interactable = false;
            authenticationCanvas.blocksRaycasts = false;
            SetMode(Mode.Move);
        }
        void OnSetMove() {
            chatInputCanvas.alpha = 0;
            chatInputCanvas.interactable = false;
            chatInputCanvas.blocksRaycasts = false;
        }

        void OnSetChat() {
            chatInputCanvas.alpha = 1;
            chatInputCanvas.interactable = true;
            chatInputCanvas.blocksRaycasts = true;
        }
    }
}
