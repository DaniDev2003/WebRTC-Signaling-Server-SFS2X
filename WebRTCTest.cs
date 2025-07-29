using Unity.WebRTC;
using UnityEngine;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Core;
using Sfs2X.Requests;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UI; // NECESARIO para RawImage
using System.Linq;


public class WebRTCTest : MonoBehaviour
{
    private Dictionary<string, RTCPeerConnection> peerConnections = new Dictionary<string, RTCPeerConnection>();
    private Dictionary<string, RTCDataChannel> dataChannels = new Dictionary<string, RTCDataChannel>();
    private List<string> processedAnswersIds = new List<string>();
 private Dictionary<string, VideoStreamTrack> receivedVideoTracks = new Dictionary<string, VideoStreamTrack>();
    Dictionary<string, HashSet<string>> appliedIceCandidates = new Dictionary<string, HashSet<string>>();

    public ChatUIManager chatUIManager;

    private string myName;
    private string roomCode;
    private MediaStream localStream;
    private AudioStreamTrack audioTrack;
    private VideoStreamTrack videoTrack;
    public bool audioEnabled = true;
    public bool videoEnabled = true;
    private bool localMediaReady = false;
    private List<string> lastJugadoresSnapshot = new List<string>();
    [SerializeField] private RawImage sourceImage;
       public GameObject cameraOffObj;

    void Start()
    {
        StartCoroutine(WebRTC.Update());

        SmartFoxConnection.SFS.AddEventListener(SFSEvent.ROOM_VARIABLES_UPDATE, OnRoomVariablesUpdate);
        SmartFoxConnection.SFS.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);

        myName = PlayerPrefs.GetString("Mynombre");
        roomCode = PlayerPrefs.GetString("SavedRoomCode");

        // <=== NUEVO: Revisar ofertas existentes en la sala para mí
        Room currentRoom = SmartFoxConnection.SFS.RoomManager.GetRoomByName(roomCode);
        if (currentRoom != null && currentRoom.ContainsVariable("jugadores"))
        {
            SFSArray jugadoresArray = (SFSArray)currentRoom.GetVariable("jugadores").Value;

            foreach (SFSObject jugador in jugadoresArray)
            {
                string ownerId = jugador.GetUtfString("ownerId");
                if (ownerId == myName) continue;

                if (jugador.ContainsKey("sdpOffers"))
                {
                    ISFSArray offers = jugador.GetSFSArray("sdpOffers");
                    for (int i = 0; i < offers.Size(); i++)
                    {
                        ISFSObject offer = offers.GetSFSObject(i);
                        string target = offer.GetUtfString("for");
                        if (target == myName && !peerConnections.ContainsKey(ownerId))
                        {
                            StartCoroutine(HandleIncomingOffer(jugador, offer));
                        }
                    }
                }
            }
        }
        // <=== NUEVO: Repetir aplicación de ICE cada segundo
        InvokeRepeating(nameof(CheckAndApplyPendingIceCandidates), 1f, 0.5f);
        StartCoroutine(InitializeLocalMediaCoroutine());
    }


    private void CheckAndApplyPendingIceCandidates()  // <=== NUEVO
    {
        Room currentRoom = SmartFoxConnection.SFS.RoomManager.GetRoomByName(roomCode);
        if (currentRoom == null || !currentRoom.ContainsVariable("jugadores")) return;

        SFSArray jugadoresArray = (SFSArray)currentRoom.GetVariable("jugadores").Value;

        foreach (SFSObject jugador in jugadoresArray)
        {
            string ownerId = jugador.GetUtfString("ownerId");
            if (ownerId == myName) continue;

            if (!peerConnections.ContainsKey(ownerId)) continue;
            var pc = peerConnections[ownerId];

            if (jugador.ContainsKey("iceCandidates"))
            {
                ISFSArray iceArray = jugador.GetSFSArray("iceCandidates");

                for (int i = 0; i < iceArray.Size(); i++)
                {
                    ISFSObject ice = iceArray.GetSFSObject(i);

                    if (ice.ContainsKey("for") && ice.GetUtfString("for") != myName)
                        continue;

                    if (!appliedIceCandidates.ContainsKey(ownerId))
                        appliedIceCandidates[ownerId] = new HashSet<string>();

                    var appliedSet = appliedIceCandidates[ownerId];

                    string candidateStr = ice.GetUtfString("candidate");
                    if (appliedSet.Contains(candidateStr))
                        continue;

                    appliedSet.Add(candidateStr);

                    // Aplicás el candidate
                    RTCIceCandidateInit candidateInit = new RTCIceCandidateInit
                    {
                        candidate = candidateStr,
                        sdpMid = ice.GetUtfString("sdpMid"),
                        sdpMLineIndex = ice.GetInt("sdpMLineIndex")
                    };

                    pc.AddIceCandidate(new RTCIceCandidate(candidateInit));
                }
            }
        }
    }
    private void OnUserEnterRoom(BaseEvent evt)
    {
        User newUser = (User)evt.Params["user"];
        string newUserName = newUser.Name;

        if (newUserName == myName) return;

        StartCoroutine(CreateOfferFor(newUserName));
    }
        private IEnumerator CreateOfferFor(string targetName)
        {
            var config = new RTCConfiguration
            {
                iceServers = new[] {
                    new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } }, // Opcional, para descubrimiento NAT
                    new RTCIceServer
                    {
                        urls = new[] { "turn:18.230.188.38:3478" },
                        username = "testuser",        // el usuario que definiste en turnserver.conf
                        credential = "testpassword"   // la contraseña que definiste
                    }
                }
            };

            var pc = new RTCPeerConnection(ref config);
            peerConnections[targetName] = pc;

                var receiveStream = new MediaStream();
                SetupVideoReceiver(receiveStream, targetName);
                SetupAudioReceiver(receiveStream, targetName);

            pc.OnTrack += e =>
            {
                receiveStream.AddTrack(e.Track);
            };
        AddLocalTracksToConnection(pc);

            pc.OnIceCandidate = candidate =>
        {

            var candidateData = new SFSObject();
            candidateData.PutUtfString("candidate", candidate.Candidate);
            candidateData.PutUtfString("sdpMid", candidate.SdpMid);
            candidateData.PutInt("sdpMLineIndex", candidate.SdpMLineIndex ?? 0);

            var iceCandidateWrapper = new SFSObject();
            iceCandidateWrapper.PutUtfString("for", targetName);
            iceCandidateWrapper.PutSFSObject("candidate", candidateData);

            var data = new SFSObject();
            data.PutUtfString("codigo", roomCode);
            data.PutUtfString("ownerId", myName);
            data.PutSFSObject("iceCandidate", iceCandidateWrapper);

            SmartFoxConnection.SFS.Send(new ExtensionRequest("WebRTC", data));
        };

            pc.OnIceConnectionChange = state =>
        {
            //posible implementacion a reconexion
                        chatUIManager.AddMessage($"{state}");
        };

            var dc = pc.CreateDataChannel("chat", new RTCDataChannelInit());
            SetupDataChannel(dc, targetName);


            var offerOp = pc.CreateOffer();
            yield return offerOp;

            if (offerOp.IsError) yield break;

            var desc = offerOp.Desc;
            var setLocalOp = pc.SetLocalDescription(ref desc);
            yield return setLocalOp;

            if (setLocalOp.IsError)
            {
                yield break;
            }
            var offerEntry = new SFSObject();
            offerEntry.PutUtfString("for", targetName);
            offerEntry.PutUtfString("sdpOffer", desc.sdp);

            var data = new SFSObject();
            data.PutUtfString("codigo", roomCode);
            data.PutUtfString("ownerId", myName);
            data.PutSFSObject("sdpOffer", offerEntry);


            SmartFoxConnection.SFS.Send(new ExtensionRequest("WebRTC", data));
        }
    private void OnRoomVariablesUpdate(BaseEvent evt)
    {

        Room room = (Room)evt.Params["room"];
        List<string> changedVars = (List<string>)evt.Params["changedVars"];

        if (!changedVars.Contains("jugadores"))
        {
            return;
        }

        SFSArray jugadoresArray = (SFSArray)room.GetVariable("jugadores").Value;

        foreach (SFSObject jugador in jugadoresArray)
        {
            string ownerId = jugador.GetUtfString("ownerId");
            string offerId = $"{ownerId}_{myName}";

            if (ownerId == myName)
            {
                if (jugador.ContainsKey("answers"))
                {
                    ISFSArray answers = jugador.GetSFSArray("answers");

                    for (int j = 0; j < answers.Size(); j++)
                    {
                        ISFSObject ans = answers.GetSFSObject(j);
                        string fromId = ans.GetUtfString("fromId");

                        if (processedAnswersIds.Contains(fromId))
                        {
                            continue;
                        }

                        processedAnswersIds.Add(fromId);

                        if (!peerConnections.ContainsKey(fromId))
                        {
                            continue;
                        }

                        string sdpAnswer = ans.GetUtfString("sdpAnswer");

                        var desc = new RTCSessionDescription
                        {
                            type = RTCSdpType.Answer,
                            sdp = sdpAnswer
                        };

                        var pc = peerConnections[fromId];
                        StartCoroutine(WaitForRemoteDescription(pc.SetRemoteDescription(ref desc)));
                    }
                }
            }
            else
            {
                if (jugador.ContainsKey("sdpOffers"))
                {
                    ISFSArray offers = jugador.GetSFSArray("sdpOffers");

                    for (int i = 0; i < offers.Size(); i++)
                    {
                        ISFSObject offer = offers.GetSFSObject(i);
                        string target = offer.GetUtfString("for");

                        if (target == myName && !peerConnections.ContainsKey(ownerId))
                        {
                            StartCoroutine(HandleIncomingOffer(jugador, offer));
                        }
                    }
                }
            }
        }
    }

    private void SetupDataChannel(RTCDataChannel channel, string remoteId)
    {
        dataChannels[remoteId] = channel;

        channel.OnMessage += bytes =>
        {
            string msg = System.Text.Encoding.UTF8.GetString(bytes);

            chatUIManager.AddMessage(msg);
        };
    }
    
    private void SetupVideoReceiver(MediaStream receiveStream, string targetName)
    {
        receiveStream.OnAddTrack += ev =>
        {
            if (ev.Track is VideoStreamTrack videoTrack)
            {
                Debug.Log($"🔗 OnTrack Kind={ev.Track.Kind} Id={ev.Track.Id} para {targetName}");
                receivedVideoTracks[targetName] = videoTrack;
                StartCoroutine(EnsureBoxAndAssignTexture(targetName, videoTrack));
            }
        };
    }

    private IEnumerator EnsureBoxAndAssignTexture(string targetName, VideoStreamTrack videoTrack)
    {
        GameObject original;
        while ((original = GameObject.Find("VideoBox")) == null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        string boxName = $"VideoBox_{targetName}";
        GameObject box = GameObject.Find(boxName);
        if (box == null)
        {
            box = Instantiate(original, original.transform.parent);
            box.name = boxName;
            chatUIManager.AjustarGridLayout();
        }

        while (!box.activeInHierarchy)
        {
            yield return new WaitForSeconds(0.5f);
        }

        Transform pv = box.transform.Find("PlayerVideo");
        if (pv == null)
        {
            yield break;
        }

        var rawImage = pv.GetComponent<RawImage>();
        if (rawImage == null)
        {
            yield break;
        }

        // 1. Espera hasta que la textura esté disponible
        while (videoTrack.Texture == null)
        {
            yield return null;
        }

        // 2. Asigna la textura (una vez)
        rawImage.texture = videoTrack.Texture;

        // 3. Asegura que si se actualiza la textura, siga funcionando
        videoTrack.OnVideoReceived += tex =>
        {
            // Esto se llama cada vez que llega un nuevo frame
            if (rawImage.texture != videoTrack.Texture)
            {
                rawImage.texture = videoTrack.Texture;
            }
        };
    }

    private IEnumerator HandleIncomingOffer(SFSObject jugador, ISFSObject offerObj)
    {
        string otherId = jugador.GetUtfString("ownerId");
        string sdpOffer = offerObj.GetUtfString("sdpOffer");

        var config = new RTCConfiguration
        {
            iceServers = new[] {
                new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } },
                new RTCIceServer
                {
                    urls = new[] { "turn:18.230.188.38:3478" },
                    username = "testuser",        // el usuario que definiste en turnserver.conf
                    credential = "testpassword"   // la contraseña que definiste
                }
            }
        };

        var pc = new RTCPeerConnection(ref config);
        if (peerConnections.ContainsKey(otherId))
        {
            yield break;
        }
        peerConnections[otherId] = pc;

        var receiveStream = new MediaStream();
        SetupVideoReceiver(receiveStream, otherId);
        SetupAudioReceiver(receiveStream, otherId);

        pc.OnTrack += e =>
        {
            receiveStream.AddTrack(e.Track);

        };
        AddLocalTracksToConnection(pc);

        pc.OnIceConnectionChange = state =>
        {
            {
                //posible reconexion
                Debug.Log($"🧊 ICE Connection State: {state}");
                            chatUIManager.AddMessage($"{state}");
            }
            ;
        };
        pc.OnIceCandidate = candidate =>
        {
            var candidateData = new SFSObject();
            candidateData.PutUtfString("candidate", candidate.Candidate);
            candidateData.PutUtfString("sdpMid", candidate.SdpMid);
            candidateData.PutInt("sdpMLineIndex", candidate.SdpMLineIndex ?? 0);

            var iceCandidateWrapper = new SFSObject();
            iceCandidateWrapper.PutUtfString("for", otherId);
            iceCandidateWrapper.PutSFSObject("candidate", candidateData);

            var data = new SFSObject();
            data.PutUtfString("codigo", roomCode);
            data.PutUtfString("ownerId", myName);
            data.PutSFSObject("iceCandidate", iceCandidateWrapper);

            SmartFoxConnection.SFS.Send(new ExtensionRequest("WebRTC", data));
        };

        pc.OnDataChannel = channel =>
        {
            SetupDataChannel(channel, otherId);
        };

        var remoteDesc = new RTCSessionDescription
        {
            type = RTCSdpType.Offer,
            sdp = sdpOffer
        };

        var setRemoteOp = pc.SetRemoteDescription(ref remoteDesc);
        yield return setRemoteOp;

        if (setRemoteOp.IsError)
        {
            yield break;
        }

        var answerOp = pc.CreateAnswer();
        yield return answerOp;

        if (answerOp.IsError) yield break;

        var desc = answerOp.Desc;
        var localDescOp = pc.SetLocalDescription(ref desc);
        yield return localDescOp;

        if (localDescOp.IsError)
        {
            yield break;
        }

        var answerObj = new SFSObject();
        answerObj.PutUtfString("fromId", myName);
        answerObj.PutUtfString("sdpAnswer", desc.sdp);

        var data = new SFSObject();
        data.PutUtfString("codigo", roomCode);
        data.PutUtfString("ownerId", otherId);
        data.PutSFSObject("answer", answerObj);

        SmartFoxConnection.SFS.Send(new ExtensionRequest("WebRTC", data));

        ApplyIceCandidatesFrom(jugador, pc);
    }

    private void SetupAudioReceiver(MediaStream receiveStream, string targetName)
    {
        receiveStream.OnAddTrack += e =>
        {
            if (e.Track is AudioStreamTrack audioTrack)
            {
                // Crear GameObject dedicado a este peer
                string receiverObjectName = $"AudioReceiver_{targetName}";

                // Verificar si ya existe uno (para evitar duplicados)
                GameObject existing = GameObject.Find(receiverObjectName);
                if (existing != null)
                {
                    Destroy(existing);
                }

                GameObject receiverObj = new GameObject(receiverObjectName);
                receiverObj.transform.SetParent(this.transform);

                // Agregar y configurar AudioSource
                AudioSource source = receiverObj.AddComponent<AudioSource>();
                source.SetTrack(audioTrack);
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 2D sound

                // Reproducir el audio
                source.Play();
            }
        };
    }

    private void ApplyIceCandidatesFrom(SFSObject jugador, RTCPeerConnection pc)
    {
        if (!jugador.ContainsKey("iceCandidates")) return;

        ISFSArray iceArray = jugador.GetSFSArray("iceCandidates");

        for (int i = 0; i < iceArray.Size(); i++)
        {
            ISFSObject ice = iceArray.GetSFSObject(i);

            if (ice.ContainsKey("for") && ice.GetUtfString("for") != myName)
                continue;

            RTCIceCandidateInit candidateInit = new RTCIceCandidateInit
            {
                candidate = ice.GetUtfString("candidate"),
                sdpMid = ice.GetUtfString("sdpMid"),
                sdpMLineIndex = ice.GetInt("sdpMLineIndex")
            };

            pc.AddIceCandidate(new RTCIceCandidate(candidateInit));
        }
    }
 
    private IEnumerator WaitForRemoteDescription(RTCSetSessionDescriptionAsyncOperation op)
{
    yield return op;

    if (op.IsError)
        Debug.LogError("Error aplicando SDP answer: " + op.Error.message);
}

    public void SendChatMessage(string message)
{
    string fullMessage = $"{myName}: {message}";

    foreach (var kvp in dataChannels)
    {
        var channel = kvp.Value;
        if (channel.ReadyState == RTCDataChannelState.Open)
        {
            channel.Send(fullMessage);
        }
    }

    ChatUIManager.Instance.AddMessage(fullMessage);
}

    private IEnumerator InitializeLocalMediaCoroutine()
    { 
        localStream = new MediaStream();

        Camera cam = GetComponent<Camera>();

                if (cam.targetTexture == null)
        {
            RenderTexture renderTexture = new RenderTexture(1280, 720, 0);
            cam.targetTexture = renderTexture;
        }
        
       videoTrack = cam.CaptureStreamTrack(1280, 720);
       sourceImage.texture = cam.targetTexture;
       localStream.AddTrack(videoTrack);

        if (Microphone.devices.Length > 0)
        {
            var micName = Microphone.devices[0];
            var audioSenderObj = GameObject.Find("WebRTC");

            var micAudioSource = audioSenderObj.AddComponent<AudioSource>();
            micAudioSource.clip = Microphone.Start(micName, true, 1, 48000);
            micAudioSource.loop = true;

            while (Microphone.GetPosition(micName) <= 0)
                yield return null;

            micAudioSource.Play();

            var audioSender = audioSenderObj.AddComponent<AudioSender>();
            audioTrack = new AudioStreamTrack(micAudioSource); 
            audioSender.track = audioTrack;
            localStream.AddTrack(audioTrack);
        }
        localMediaReady = true;
    }

    public void AddLocalTracksToConnection(RTCPeerConnection pc)
    {
        if (localStream == null) return;

        foreach (var track in localStream.GetTracks())
        {
            if (track != null)
            {
                Debug.Log($"📡 Agregando track local: {track.Kind}");
                pc.AddTrack(track, localStream);
            }
        }
    }

public void ToggleVideo()
{
    videoEnabled = !videoEnabled;
    cameraOffObj.SetActive(!videoEnabled);
}

    public void ToggleAudio()
    {
        if (localStream == null) return;

        audioEnabled = !audioEnabled;

        var audioSender = GameObject.Find("WebRTC")?.GetComponent<AudioSender>();
        if (audioSender != null)
        {
            audioSender.muteAudio = !audioEnabled; 
        }
    }
    void OnDestroy()
    {
        foreach (var dc in dataChannels.Values) dc?.Close();
        foreach (var pc in peerConnections.Values) pc?.Close();

        SmartFoxConnection.SFS.RemoveEventListener(SFSEvent.ROOM_VARIABLES_UPDATE, OnRoomVariablesUpdate);
        SmartFoxConnection.SFS.RemoveEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
    }
}
class AudioSender : MonoBehaviour
{
    public AudioStreamTrack track;
    const int sampleRate = 48000;
    public bool muteAudio = false;
    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (track != null)
        {
            if (muteAudio)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = 0;
                }
            }

            track.SetData(data, channels, sampleRate);
        }
    }
}

