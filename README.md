# My Steelport emulator

An emulator for the My Steelport service on saintsrow.com that is now sadly
discontinued.

Originally implemented in late March/early April 2015 as a service to run on
saintsrowmods.com. Unfortunately to do auth securely it needs to be able to
authenticate encrypted steam tokens and you can't do this without being
the Steamworks partner who actually built the game.

Nastily hacked up into a standalone app to run on local machines in ~6 hours in
May 2019.

## Setup Instructions

To use this emulator, you need to redirect the game's traffic to your local machine.

### 1. Configure the Hosts File
You must point `sr3.hydra.agoragames.com` to your local IP address (usually `127.0.0.1`).

**On Windows:**
1. Open Notepad as Administrator.
2. Open `C:\Windows\System32\drivers\etc\hosts`.
3. Add the following line at the end of the file:
   ```
   127.0.0.1 sr3.hydra.agoragames.com
   ```
4. Save the file.

### 2. Ensure Port 443 is Available
The emulator listens on port 443 (HTTPS). Make sure no other service (like IIS, Skype, or another web server) is using this port.

### 3. Run the Emulator
1. Build the solution using Visual Studio or run the compiled `MySteelportEmulator.exe`.
2. The console should indicate that it's listening on the redirected IP.

## Troubleshooting

- If you see an error about port 443 being in use, you may need to stop services like `World Wide Web Publishing Service` (IIS) or close apps that use port 443.
- If the game still can't connect, double check your hosts file entry by running `ping sr3.hydra.agoragames.com` in a command prompt; it should resolve to `127.0.0.1`.

## Forum Thread
For more information and community support, visit the [official forum thread](https://www.saintsrowmods.com/forum/threads/my-steelport-emulator.17361/).

## Credits

AaltoTLS is Copyright (C) 2010-2011 Aalto University. (Used in the original 2015 implementation, but since replaced by BouncyCastle).

BouncyCastle is used for TLS support.