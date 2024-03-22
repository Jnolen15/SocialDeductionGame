using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using VivoxUnity;
using System.Threading.Tasks;
using System;

public class VivoxManager : MonoBehaviour
{
    // ============== Singleton pattern ==============
    #region Singleton
    public static VivoxManager Instance { get; private set; }
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        DontDestroyOnLoad(gameObject);
    }
    #endregion

    // ============== Variables ==============
    #region Variables
    private bool _isMidInitialize;
    private bool _hasInitialized;

    private Client _client;
    public ILoginSession LoginSession;
    private string _lobbyID;
    private IChannelSession _lobbyChannelSession = null;
    private IChannelSession _worldChannelSession = null;
    private IChannelSession _deathChannelSession = null;
    private IChannelSession _saboChannelSession = null;

    public enum ChannelSeshName
    {
        Lobby,
        World,
        Death,
        Sabo,
        Unknown // Not a real channel used when channel not found
    }

    public delegate void VivoxAction();
    public static event VivoxAction OnLoginSuccess;
    public static event VivoxAction OnLoginFailure;
    public static event VivoxAction OnDeathChannelJoined;

    public delegate void SpeakingAction(string displayName, VivoxManager.ChannelSeshName channelName);
    public static event SpeakingAction OnVoiceInputStarted;
    public static event SpeakingAction OnVoiceInputEnded;
    #endregion

    // ============== Setup ==============
    #region Setup
    private void Awake()
    {
        InitializeSingleton();
    }

    private void OnDestroy()
    {
        // Leave voice if going direct to main menu.
        // This for sure has to be fixed in a better way at some point
        //LeaveLobbyChannel();
        //LeaveWorldChannel();
        //LeaveDeathChannel();
    }
    #endregion

    // ============== Login ==============
    #region Login
    public void VivoxLogin()
    {
        if (_isMidInitialize)
            return;
        _isMidInitialize = true;

        Debug.Log("<color=green>VIVOX: </color>Attempting Vivox login!");

        VivoxService.Instance.Initialize();

        string playerName = PlayerPrefs.GetString(PlayerNamer.KEY_PLAYERNAME);
        if (playerName == null)
            playerName = "Player";

        Account account = new Account(playerName);

        _client = VivoxService.Instance.Client;
        LoginSession = _client.GetLoginSession(account);

        LoginSession.BeginLogin(LoginSession.GetLoginToken(), SubscriptionMode.Accept, null, null, null, result =>
        {
            try
            {
                LoginSession.EndLogin(result);
                _hasInitialized = true;
                OnLoginSuccess?.Invoke();
                Debug.Log("<color=green>VIVOX: </color>login complete!");
            }
            catch (VivoxApiException e)
            {
                // Unbind any login session-related events you might be subscribed to.
                // Handle error
                OnLoginFailure?.Invoke();
                Debug.LogError($"<color=green>VIVOX: </color>Could not login: {e.Message}");
                return;
            }
            finally
            {
                _isMidInitialize = false;
            }
            // At this point, we have successfully requested to login. 
            // When you are able to join channels, LoginSession.State will be set to LoginState.LoggedIn.
            // Reference LoginSession_PropertyChanged()
        });
    }
    #endregion

    // ============== Join ==============
    #region Join
    public void JoinWorldChannel(string lobbyId)
    {
        Debug.Log("<color=green>VIVOX: </color>Attempting to join Vivox world channel!");

        ChannelType posChannelType = ChannelType.Positional;
        Channel3DProperties channel3DProperties = new Channel3DProperties(32, 30, 1f, AudioFadeModel.InverseByDistance);
        Channel posChannel = new Channel(lobbyId + "_world", posChannelType, channel3DProperties);

        JoinVivoxChannel(posChannel, false, ChannelSeshName.World);
    }

    public void JoinLobbyChannel(string lobbyId)
    {
        Debug.Log("<color=green>VIVOX: </color>Attempting to join Vivox lobby channel!");

        _lobbyID = lobbyId;

        ChannelType lobbyChannelType = ChannelType.NonPositional;
        Channel lobbyChannel = new Channel(lobbyId + "_lobby", lobbyChannelType, null);

        JoinVivoxChannel(lobbyChannel, false, ChannelSeshName.Lobby);
    }

    public void JoinDeathChannel()
    {
        Debug.Log("<color=green>VIVOX: </color>Attempting to join Vivox death channel!");

        ChannelType deathChannelType = ChannelType.NonPositional;
        Channel deathChannel = new Channel(_lobbyID + "_death", deathChannelType, null);

        JoinVivoxChannel(deathChannel, false, ChannelSeshName.Death);
    }

    public void JoinSaboChannel()
    {
        Debug.Log("<color=green>VIVOX: </color>Attempting to join Vivox saboteur channel!");

        ChannelType saboChannelType = ChannelType.NonPositional;
        Channel saboChannel = new Channel(_lobbyID + "_sabo", saboChannelType, null);

        JoinVivoxChannel(saboChannel, false, ChannelSeshName.Sabo);
    }

    public void JoinVivoxChannel(Channel channel, bool switchTransmission, ChannelSeshName channelSessionName)
    {
        if (!_hasInitialized || LoginSession.State != LoginState.LoggedIn)
        {
            Debug.LogWarning("<color=green>VIVOX: </color>Can't join a Vivox audio channel, as Vivox login hasn't completed yet.");
            return;
        }

        IChannelSession channelSession = LoginSession.GetChannelSession(channel);
        string token = channelSession.GetConnectToken();

        channelSession.BeginConnect(true, false, switchTransmission, token, result =>
        {
            try
            {
                // Special case: It's possible for the player to leave the lobby between the time we called BeginConnect and the time we hit this callback.
                // If that's the case, we should abort the rest of the connection process.
                if (channelSession.ChannelState == ConnectionState.Disconnecting ||
                    channelSession.ChannelState == ConnectionState.Disconnected)
                {
                    Debug.LogWarning("<color=green>VIVOX: </color>Vivox channel is already disconnecting. Terminating the channel connect sequence.");
                    HandleEarlyDisconnect(channelSession);
                    return;
                }

                channelSession.EndConnect(result);

                if (channelSessionName == ChannelSeshName.Lobby)
                    _lobbyChannelSession = channelSession;
                else if (channelSessionName == ChannelSeshName.World)
                {
                    _worldChannelSession = channelSession;
                    _worldChannelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
                }
                else if (channelSessionName == ChannelSeshName.Death)
                {
                    _deathChannelSession = channelSession;
                    OnDeathChannelJoined?.Invoke();
                    _deathChannelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
                }
                else if (channelSessionName == ChannelSeshName.Sabo)
                {
                    _saboChannelSession = channelSession;
                    _saboChannelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
                }

                Debug.Log("<color=green>VIVOX: </color>Joined Vivox channel " + channel.Name);
            }
            catch (VivoxApiException e)
            {
                Debug.LogError($"<color=green>VIVOX: </color>Could not connect to channel: {e.Message}");
                return;
            }
        });
    }
    #endregion

    // ============== Leave ==============
    #region Leave
    public void LeaveLobbyChannel()
    {
        Debug.Log("<color=green>VIVOX: </color>Leaving Vivox lobby channel!");

        LeaveChannel(_lobbyChannelSession);
    }

    public void LeaveWorldChannel()
    {
        Debug.Log("<color=green>VIVOX: </color>Leaving Vivox world channel!");

        if (_worldChannelSession != null)
            _worldChannelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;

        LeaveChannel(_worldChannelSession);
    }

    public void LeaveDeathChannel()
    {
        Debug.Log("<color=green>VIVOX: </color>Leaving Vivox death channel!");

        if (_deathChannelSession != null)
            _deathChannelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;

        LeaveChannel(_deathChannelSession);
    }

    public void LeaveSaboChannel()
    {
        Debug.Log("<color=green>VIVOX: </color>Leaving Vivox sabo channel!");

        if (_saboChannelSession != null)
            _saboChannelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;

        LeaveChannel(_saboChannelSession);
    }

    public void LeaveAll()
    {
        LeaveLobbyChannel();
        LeaveWorldChannel();
        LeaveDeathChannel();
        LeaveSaboChannel();
    }

    public void LeaveChannel(IChannelSession channelSession)
    {
        Debug.Log("<color=green>VIVOX: </color>Attempting to leaving channel " + channelSession);

        if (channelSession != null)
        {
            // Disconnect from channel
            channelSession.Disconnect();

            Debug.Log("<color=green>VIVOX: </color>Leaving channel " + channelSession);
        }

        /*if (channelSession != null)
        {
            // Special case: The EndConnect call requires a little bit of time before the connection actually completes, but the player might
            // disconnect before then. If so, sending the Disconnect now will fail, and the played would stay connected to voice while no longer
            // in the lobby. So, wait until the connection is completed before disconnecting in that case.
            if (channelSession.ChannelState == ConnectionState.Connecting)
            {
                Debug.LogWarning("<color=green>VIVOX: </color>Vivox channel is trying to disconnect while trying to complete its connection. Will wait until connection completes.");
                HandleEarlyDisconnect(channelSession);
                return;
            }

            ChannelId id = channelSession.Channel;
            channelSession?.Disconnect(
                (result) =>
                {
                    LoginSession.DeleteChannelSession(id);
                    channelSession = null;
                });
        }*/
    }

    private void HandleEarlyDisconnect(IChannelSession channelSession)
    {
        DisconnectOnceConnected(channelSession);
    }

    async void DisconnectOnceConnected(IChannelSession channelSession)
    {
        while (channelSession?.ChannelState == ConnectionState.Connecting)
        {
            await Task.Delay(200);
            return;
        }

        LeaveChannel(channelSession);
    }
    #endregion

    // ============== Other ==============
    #region Other
    public void SetTransmissionAll()
    {
        //Debug.Log("<color=green>VIVOX: </color>Setting Transimison mode to all");
        LoginSession.SetTransmissionMode(TransmissionMode.All);

        /*foreach (ChannelId id in LoginSession.TransmittingChannels)
        {
            Debug.Log("<color=green>VIVOX: </color>IN CHANNEL " + id);
        }*/
    }

    public void SetTransmissionChannel(ChannelSeshName channelName)
    {
        //Debug.Log("<color=green>VIVOX: </color>Setting Transimison mode to single " + channelName);
        if(channelName == ChannelSeshName.World)
        {
            LoginSession.SetTransmissionMode(TransmissionMode.Single, _worldChannelSession.Channel);
        }
        else if (channelName == ChannelSeshName.Lobby)
        {
            LoginSession.SetTransmissionMode(TransmissionMode.Single, _lobbyChannelSession.Channel);
        }
        else if (channelName == ChannelSeshName.Death)
        {
            LoginSession.SetTransmissionMode(TransmissionMode.Single, _deathChannelSession.Channel);
        }
        else if (channelName == ChannelSeshName.Sabo)
        {
            LoginSession.SetTransmissionMode(TransmissionMode.Single, _saboChannelSession.Channel);
        }
        else
        {
            Debug.Log("<color=green>VIVOX: </color>Channel not set or recognized in SetTransmissionChannel");
        }
    }

    public void SetTransmissionNone()
    {
        //Debug.Log("<color=green>VIVOX: </color>Setting Transimison mode to none");
        LoginSession.SetTransmissionMode(TransmissionMode.None);
    }

    public void UpdateWorldChannelPosition(Transform listener, Transform speaker)
    {
        Debug.Log("<color=green>VIVOX: </color>Updating players 3D postion. Speaker: " + speaker.position.x + 
                    ", " + speaker.position.y + ", " + speaker.position.z +
                    "Listener: " + listener.position.x +  ", " + listener.position.y + ", " + listener.position.z);

        if(_worldChannelSession.AudioState == ConnectionState.Connected)
        {
            _worldChannelSession.Set3DPosition(speaker.position, listener.position,
                                                listener.forward, listener.up);
        }
        else
        {
            Debug.LogError("Tried to set world channel position but was not yet connected to world channel, state: " + _worldChannelSession.AudioState);
        }
    }

    public Client GetClientData()
    {
        return _client;
    }

    private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> valueEventArg)
    {
        ValidateArgs(new object[] { sender, valueEventArg }); //see code from earlier in post

        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        var participant = source[valueEventArg.Key];

        string username = valueEventArg.Value.Account.Name;
        string displayName = valueEventArg.Value.Account.DisplayName;
        ChannelId channel = valueEventArg.Value.ParentChannelSession.Key;
        string property = valueEventArg.PropertyName;

        //Debug.Log("<color=green>VIVOX: </color> OnParticipantValueUpdated " + property);

        switch (property)
        {
            case "SpeechDetected":
                {
                    if (participant.SpeechDetected)
                    {
                        //Debug.Log($"<color=green>VIVOX: </color>Detecting player {displayName} speach in {channel.Name}!");
                        OnVoiceInputStarted?.Invoke(displayName, GetChannelSeshName(channel.Name));
                    }
                    else
                    {
                        //Debug.Log($"<color=green>VIVOX: </color>Player {displayName} speach in {channel.Name} ended!");
                        OnVoiceInputEnded?.Invoke(displayName, GetChannelSeshName(channel.Name));
                    }
                    break;
                }
            default:
                break;
        }
    }

    private ChannelSeshName GetChannelSeshName(string channelName)
    {
        if (_worldChannelSession != null && channelName == _worldChannelSession.Channel.Name)
        {
            return ChannelSeshName.World;
        }
        else if (_saboChannelSession != null && channelName == _saboChannelSession.Channel.Name)
        {
            return ChannelSeshName.Sabo;
        }
        else if (_deathChannelSession != null && channelName == _deathChannelSession.Channel.Name)
        {
            return ChannelSeshName.Death;
        }
        else
        {
            Debug.LogWarning("GetChannelSeshName did not find matching channel, returning unknown");
            return ChannelSeshName.Unknown;
        }
    }
    #endregion

    // ============== Settings ==============
    #region Settings
    public void AdjustInputVolume(int value)
    {
        IAudioDevices devices = _client.AudioInputDevices;

        if (value > 50)
            value = 50;
        else if (value < -50)
            value = -50;

        // Refresh list of devices to have it up to date
        var ar = devices.BeginRefresh(new AsyncCallback((IAsyncResult result) =>
        {
            // Set the volume for the device
            devices.VolumeAdjustment = value;
        }));
    }

    public void SetInputDevice(IAudioDevice targetInput = null)
    {
        IAudioDevices inputDevices = _client.AudioInputDevices;
        if (targetInput != null && targetInput != _client.AudioInputDevices.ActiveDevice)
        {
            Debug.Log("<color=green>VIVOX: </color>Swapping Input device " + targetInput.Name);

            _client.AudioInputDevices.BeginSetActiveDevice(targetInput, ar =>
            {
                if (ar.IsCompleted)
                {
                    _client.AudioInputDevices.EndSetActiveDevice(ar);
                }
            });
        }
    }

    public void SetOutputDevice(IAudioDevice targetOutput = null)
    {
        IAudioDevices outputDevices = _client.AudioOutputDevices;
        if (targetOutput != null && targetOutput != _client.AudioOutputDevices.ActiveDevice)
        {
            Debug.Log("<color=green>VIVOX: </color>Swapping Output device " + targetOutput.Name);

            _client.AudioOutputDevices.BeginSetActiveDevice(targetOutput, ar =>
            {
                if (ar.IsCompleted)
                {
                    _client.AudioOutputDevices.EndSetActiveDevice(ar);
                }
            });
        }
    }
    #endregion

    // ============== DEBUG ==============
    public ConnectionState GetChannelState(IChannelSession channelSession)
    {
        return channelSession.ChannelState;
    }

    private static void ValidateArgs(object[] objs)
    {
        foreach (var obj in objs)
        {
            if (obj == null)
                throw new System.ArgumentNullException(obj.GetType().ToString(), "Specify a non-null/non-empty argument.");
        }
    }
}
