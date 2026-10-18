using System.Runtime.InteropServices;
using Xunit;

namespace Game.MonoGame.Session.Tests;

public sealed class SteamLibraryTests
{
    [Fact]
    public void PinnedRedistributableLoadsAndExportsRequiredSocketFunctionsWithoutInitializingSteam()
    {
        string library = OperatingSystem.IsWindows() ? "steam_api64.dll" : "libsteam_api.so";
        IntPtr handle = NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, library));
        try
        {
            foreach (string entry in new[] { "SteamInternal_SteamAPI_Init", "SteamAPI_RunCallbacks", "SteamAPI_SteamNetworkingSockets_SteamAPI_v012", "SteamAPI_ISteamNetworkingSockets_CreateListenSocketP2P", "SteamAPI_ISteamNetworkingSockets_ConnectP2P", "SteamAPI_ISteamNetworkingSockets_SendMessageToConnection" })
                Assert.True(NativeLibrary.TryGetExport(handle, entry, out _), entry);
        }
        finally { NativeLibrary.Free(handle); }
    }
}
