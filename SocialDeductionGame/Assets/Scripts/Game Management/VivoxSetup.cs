using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using VivoxUnity;
using System.Threading.Tasks;

public class VivoxSetup : MonoBehaviour
{
    // ============== Variables ==============
    #region Variables
    private bool _isMidInitialize;
    private bool _hasInitialized;

    public ILoginSession LoginSession;
    private IChannelSession _channelSession = null;
    #endregion

    // ============== Setup ==============
    private void Awake()
    {
        //DontDestroyOnLoad(this.gameObject);
    }

    // ============== Vivox ==============
    #region Vivox
    public void VivoxLogin()
    {
        if (_isMidInitialize)
            return;
        _isMidInitialize = true;

        Debug.Log("<color=green>VIVOX: </color>Attempting Vivox login!");

        VivoxService.Instance.Initialize();

        Account account = new Account(AuthenticationService.Instance.PlayerId);

        LoginSession = VivoxService.Instance.Client.GetLoginSession(account);
        //LoginSession.PropertyChanged += VivoxLoginSession_PropertyChanged;

        LoginSession.BeginLogin(LoginSession.GetLoginToken(), SubscriptionMode.Accept, null, null, null, result =>
        {
            try
            {
                LoginSession.EndLogin(result);
                _hasInitialized = true;

                Debug.Log("<color=green>VIVOX: </color>login complete!");
            }
            catch (System.Exception e)
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

    // For this example, we immediately join a channel after LoginState changes to LoginState.LoggedIn.
    // In an actual game, when to join a channel will vary by implementation.
    /*private void VivoxLoginSession_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        var loginSession = (ILoginSession)sender;
        if (e.PropertyName == "State")
        {
            if (loginSession.State == LoginState.LoggedIn)
            {
                bool connectAudio = true;
                bool connectText = true;

                // This puts you into an echo channel where you can hear yourself speaking.
                // If you can hear yourself, then everything is working and you are ready to integrate Vivox into your project.
                JoinVivoxChannel("TestChannel", ChannelType.Echo, connectAudio, connectText);
                // To test with multiple users, try joining a non-positional channel.
                //JoinVivoxChannel("MultipleUserTestChannel", ChannelType.NonPositional, connectAudio, connectText);
            }
        }
    }*/

    public void JoinVivoxChannel(string lobbyId)
    {
        if (!_hasInitialized || LoginSession.State != LoginState.LoggedIn)
        {
            Debug.LogWarning("<color=green>VIVOX: </color>Can't join a Vivox audio channel, as Vivox login hasn't completed yet.");
            return;
        }

        Debug.Log("<color=green>VIVOX: </color>Attempting to join Vivox lobby channel!");

        ChannelType channelType = ChannelType.NonPositional;
        Channel channel = new Channel(lobbyId + "_voice", channelType, null);
        _channelSession = LoginSession.GetChannelSession(channel);
        string token = _channelSession.GetConnectToken();

        _channelSession.BeginConnect(true, false, true, token, result =>
        {
            try
            {
                // Special case: It's possible for the player to leave the lobby between the time we called BeginConnect and the time we hit this callback.
                // If that's the case, we should abort the rest of the connection process.
                if (_channelSession.ChannelState == ConnectionState.Disconnecting ||
                    _channelSession.ChannelState == ConnectionState.Disconnected)
                {
                    Debug.LogWarning("<color=green>VIVOX: </color>Vivox channel is already disconnecting. Terminating the channel connect sequence.");
                    HandleEarlyDisconnect();
                    return;
                }

                _channelSession.EndConnect(result);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"<color=green>VIVOX: </color>Could not connect to channel: {e.Message}");
                return;
            }
        });
    }

    public void LeaveLobbyChannel()
    {
        if (_channelSession != null)
        {
            // Special case: The EndConnect call requires a little bit of time before the connection actually completes, but the player might
            // disconnect before then. If so, sending the Disconnect now will fail, and the played would stay connected to voice while no longer
            // in the lobby. So, wait until the connection is completed before disconnecting in that case.
            if (_channelSession.ChannelState == ConnectionState.Connecting)
            {
                Debug.LogWarning("<color=green>VIVOX: </color>Vivox channel is trying to disconnect while trying to complete its connection. Will wait until connection completes.");
                HandleEarlyDisconnect();
                return;
            }

            ChannelId id = _channelSession.Channel;
            _channelSession?.Disconnect(
                (result) =>
                {
                    LoginSession.DeleteChannelSession(id);
                    _channelSession = null;
                });
        }
    }

    private void HandleEarlyDisconnect()
    {
        DisconnectOnceConnected();
    }

    async void DisconnectOnceConnected()
    {
        while (_channelSession?.ChannelState == ConnectionState.Connecting)
        {
            await Task.Delay(200);
            return;
        }

        LeaveLobbyChannel();
    }
    #endregion
}
