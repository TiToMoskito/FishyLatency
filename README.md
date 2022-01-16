# FishyLatency

A latency simulation transport for Fish-Net. It's a middle in the man transport, which simulates latency and packet drops. 
To make it work you need your **Main Transport**, **Transport Manager** and of course **FishyLatency**.

## Dependencies
1. **[Fish-Net](https://github.com/FirstGearGames/FishNet)**

## Setting Up

1. Install Fish-Net from the official repo (or AssetStore) **[Download Fish-Net](https://github.com/FirstGearGames/FishNet/releases)**.
2. Install FishyLatency **[unitypackage](https://github.com/TiToMoskito/FishyLatency/releases)** from the release section.
3. In your **"NetworkManager"** GameObject add a  **"Transport Manager"** script and the **"FishyLatency"** script.
4. Assign your primary transport to the  **"FishyLatency"** "Transport" variable.
5. Assign  **"FishyLatency"** to the **"Transport Manager"**.

## Tested with
1. Tugboat
2. FishyFacepunch
(It should work with any other transport out of the box, too)

## Known issues
None
