using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using VivoxUnity;
using System.Threading.Tasks;

public class VivoxManager : MonoBehaviour
{
    // ============== Singleton pattern ==============
    #region Singleton
    public static VivoxManager Instance { get; private set; }
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;

        DontDestroyOnLoad(gameObject);
    }
    #endregion

    // ============== Variables ==============
    #region Variables
    private bool _isMidInitialize;
    private bool _hasInitialized;

    public ILoginSession LoginSession;
    private IChannelSession _lobbyChannelSession = null;
    private IChannelSession _worldChannelSession = null;

    public enum ChannelSeshName
    {
        Lobby,
        World,
        Sabo
    }
    #endregion

    // ============== Setup ==============
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
    }

    // ============== Login ==============
    #region Login
    public void VivoxLogin()
    {
        if (_isMidInitialize)
            return;
        _isMidInitialize = true;

        Debug.Log("<color=green>VIVOX: </color>Attempting Vivox login!");

        VivoxService.Instance.Initialize();

        Account account = new Account(AuthenticationService.Instance.PlayerId);

        LoginSession = VivoxService.Instance.Client.GetLoginSession(account);

        LoginSession.BeginLogin(LoginSession.GetLoginToken(), SubscriptionMode.Accept, null, null, null, result =>
        {
            try
            {
                LoginSession.EndLogin(result);
                _hasInitialized = true;

                Debug.Log("<color=green>VIVOX: </color>login complete!");
            }
            catch (VivoxApiException e)
            {
                // Unbind any login session-related events you might be subscribed to.
                // Handle error
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

        ChannelType lobbyChannelType = ChannelType.NonPositional;
        Channel lobbyChannel = new Channel(lobbyId + "_lobby", lobbyChannelType, null);

        JoinVivoxChannel(lobbyChannel, true, ChannelSeshName.Lobby);
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
                    _worldChannelSession = channelSession;

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
    public void LeaveWorldChannel()
    {
        Debug.Log("<color=green>VIVOX: </color>Leaving Vivox world channel!");

        LeaveChannel(_worldChannelSession);
    }

    public void LeaveLobbyChannel()
    {
        Debug.Log("<color=green>VIVOX: </color>Leaving Vivox lobby channel!");

        LeaveChannel(_lobbyChannelSession);
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
    public void SetTransmissionAll()
    {
        Debug.Log("<color=green>VIVOX: </color>Setting Transimison mode to all");
        LoginSession.SetTransmissionMode(TransmissionMode.All);

        foreach (ChannelId id in LoginSession.TransmittingChannels)
        {
            Debug.Log("<color=green>VIVOX: </color>IN CHANNEL " + id);
        }
    }

    public void UpdateWorldChannelPosition(Transform listener, Transform speaker)
    {
        Debug.Log("<color=green>VIVOX: </color>Updating players 3D postion. Speaker: " + speaker.position.x + 
                    ", " + speaker.position.y + ", " + speaker.position.z +
                    "Listener: " + listener.position.x +  ", " + listener.position.y + ", " + listener.position.z);

        _worldChannelSession.Set3DPosition(speaker.position, listener.position,
                listener.forward, listener.up);
    }
}
