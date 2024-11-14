using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Reflection;
using ImGuiNET;
using Dalamud.IoC;
using System.Globalization;

namespace XIVLogger
{

    public class Plugin : IDalamudPlugin
    {
        public string Name => "XIVLogger";

        private const string commandName = "/xivlogger";

        [PluginService] private IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public IChatGui Chat { get; set; }
        [PluginService] public IFramework framework { get; set; }
        [PluginService] public IClientState ClientState { get; set; }
        private ICommandManager commandManager { get; init; }
        private Configuration configuration;
        public ChatLog log;
        private PluginUI ui;
        private readonly IPluginLog pluginLog;
        private bool loggingIn = false;
        private bool loggedIn = false;

        public string Location { get; private set; } = Assembly.GetExecutingAssembly().Location;

        public Plugin(ICommandManager command)
        {            
            this.configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.configuration.Initialize(PluginInterface);

            this.ui = new PluginUI(this.configuration);

            this.commandManager = command;
            commandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens settings window for XIVLogger"
            });

            commandManager.AddHandler("/savelog", new CommandInfo(OnSaveCommand)
            {
                HelpMessage = "Saves a chat log as a text file with the current settings, /savelog <number> to save the last <number> messages"
            });

            commandManager.AddHandler("/copylog", new CommandInfo(OnCopyCommand)
            {
                HelpMessage = "Copies a chat log to your clipboard with the current settings, /copylog <number> to copy the last <number> messages"
            });

            this.log = new ChatLog(configuration, PluginInterface, Chat);
            this.ui.log = log;

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += () => DrawConfigUI();
            this.ClientState.Login += OnLogin;
            this.ClientState.Logout += OnLogout;
            Chat.ChatMessage += OnChatMessage;
            this.framework.Update += OnUpdate;

        }

        private void OnLogin()
        {
            loggingIn = true;
        }

        private void OnLogout(int type, int code)
        {
            if (configuration.fAutosave && loggedIn)
            {
                log.autoSave();
                log.wipeLog();
            }
            pluginLog.Debug("Logged out!");
            loggedIn = false;
        }

        private void OnUpdate(IFramework framework)
        {

            if (loggingIn && this.ClientState.LocalPlayer != null)
            {
                loggingIn = false;
                loggedIn = true;
                log.setupAutosave(this.ClientState.LocalPlayer.Name.ToString());
            }

            if (configuration.fAutosave)
            {
                if (configuration.checkTime())
                {
                    log.autoSave();
                    configuration.updateAutosaveTime();
  
                }

            }
        }
        /*
                     OnMessageDelegate(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled);

         */
        private void OnChatMessage(XivChatType type, int id, ref SeString sender, ref SeString message, ref bool handled)
        {
            log.addMessage(type, sender.TextValue, message.TextValue);

            //PluginLog.Log("Chat message from type {0}: {1}", type, message.TextValue);
        }

        public void Dispose()
        {

            commandManager.RemoveHandler(commandName);
            commandManager.RemoveHandler("/savelog");
            commandManager.RemoveHandler("/copylog");
            
            this.framework.Update -= OnUpdate;
            Chat.ChatMessage -= OnChatMessage;
            this.ClientState.Login -= OnLogin;
            this.ClientState.Logout -= OnLogout;
        }

        private void OnCommand(string command, string args)
        {
            this.ui.SettingsVisible = true;
        }

        private void OnSaveCommand(string command, string args)
        {
            log.printLog(args);
        }

        private void OnCopyCommand(string command, string args)
        {
            ImGui.SetClipboardText(log.printLog(args, aClipboard: true));
        }


        private void DrawUI()
        {
            this.ui.Draw();
        }

        private void DrawConfigUI()
        {
            this.ui.SettingsVisible = true;
        }
    }
}
