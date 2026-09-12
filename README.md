# COMP3008 Distributed Computing - Assignment 1

## Authors

**COMP3008 Distributed Computing - Assignment 1**

Group project submission.

    Amanda Nguyen, 22223850

    Jamie Dymmott, 20727017

    Jonathan Huynh, 20596725

## Real-Time Chat Application for Windows
This project implements a distributed real-time chat application using **C#**, **WPF**, and **Windows Communication Foundation (WCF)**.

The solution supports two different client applications:

- **PollingClient** - retrieves updates from the server using periodic polling.
- **DuplexClient** - receives real-time updates through WCF duplex callbacks.

Both clients connect to the same server and can communication with each other without restarting or reconfiguring the server

---

## Solution Structure

### DataLibrary
Contains shared data/result classes used by the server and client applicaitons.

Examples include:
- Sign-in results
- Channel action results
- Chat messages
- Shared file information
- File download results

### Database
Provides the in-memory data layer used by the chat server.

Stores application data such as:
- Users
- Channels
- Messages
- Private conversations
- Shared files

No persistent database is required.

### ServerTier
Contains the WCF chat server and service contracts.

Main responsibilities:
- User sign-in and sign-out
- Channel creation, joining and leaving
- Public messaging
- Private messaging
- File sharing and downloading
- Tracking channel membership
- Supporting both polling and duplex clients
- Registering duplex callbacks
- Detecting disconnected duplex clients
- Removing disconnected users and updating remaining clients

The server exposes separate WCF endpoints for the polling and duplex clients.

### PollingClient
A WCF client that regularly requests updates form the server.

Main feature:
- Sign in and sign out
- Create and join channels
- Leave channels
- View channel members
- Send and receive public messages
- Send and receive private messages
- Share and download supported files
- Poll the server for channel, member, message and file updates

### DuplexClient
A separate WPF client that provides the same main chat functionality without using a polling timer.

The DuplexClient uses **WCF duplex communication**, allowing the server to push updates directly to connected clients.

Main callback updates include:
- Channel list changes
- Channel membership changes
- Public messages
- Shared file updates
- Private messages

The DuplexClient also:
- Uses 'Dispatcher.BeginInvoke' for safe WPF UI updates
- Automatically opens private-message windows when a new private message arrives
- Detects normal and unexpected client disconnections
- Releases disconnected user IDs
- Removes disconnected users from channel membership

---

## Main Features

- Multiple simultaneous users
- Unique user IDs
- Channel creation
- Join and leave channels
- Public channel message
- Channel member lists
- Shared files
- File download and opening
- Maximum supported files size **2 MB**
- Supported file types: 
    - '.png'
    - '.jpg'
    - '.jpeg'
    - '.gif'
    - '.bmp'
    - '.txt'
- Polling-based clients updates
- Real-time duplex callback updates
- Automatic client-disconnection cleanup

---

## How to Run

### 1. Open the Solution

Open the project solution in **Microsoft Visual Studio**.

Build the full solution:

    Ctrl + Shift + B

Ensure the build completes with no errors.

### 2. Start the Server

Run the **ServerTier** project first.

Leave the server running while using the client applications.

### 3. Start a Client

Run either:

    PollingClient

or:

    DuplexClient

Multiple client instances can be opened at the same time.

For example:

    ServerTier
    - PollingClient - User A
    - PollingClient - User B
    - DuplexClient  - User C
    - DuplexClient  - User D

All clients communicate through the same running server.

## Basic Usage

1. Start **ServerTier**
2. Open one or more client applications.
3. Enter a unique user ID and sign in.
4. Create or select a channel.
5. Join the channel.
6. Send public messages to channel members.
7. Double-click a member to open a private conversation.
8. Use the Shared Files section to upload or open supported files.
9. Leave the channel or sign out when finished.

## Communication Design

### PollingClient

The PollingClient regularly requests updated informaiton from the server, including:

- Channel lists
- Channel members
- New public messages
- Shared files
- Private converations

The general flow is:

    1. Polling Client
    2. Request server updates
    3. Server returns current data
    4. WPF interface updates
    5. Repeat after polling interval

### DuplexClient

The DuplexClient registers a callback with the server when signing in.

The server then pushes updates directly to the client:

    1. Server event
    2. WCF callback
    3. ClientUpdateHandler
    4. WPF Dispatcher
    5. User interface updated

This allows the DuplexClient to receive real-time updates without using a polling timer.

---

## Thread Safety

The server uses synchronisation locks to protect shared application state when multiple clients access the server concurrently.

WCF duplex callbacks can arrive on background communication threads.

WPF controls must only be updated from the UI thread, so the DuplexClient uses:

    Dispatcher.BeginInvoke(...)

to safely update the interface.

---

## Disconnection Handling

The server monitors DuplexClient callback channels for 'Faulted' and 'Closed' events.

If a DuplexClient disconnects unexpectedly, the server:

1. Removes its callback.
2. Removes the user from their channel.
3. Releases the user ID.
4. Updates the member list for remaining clients.

This allows the disconnected user ID to be reused without restarting the server.

---

## Technologies Used

- C#
- .NET Framework
- WPF
- Windows Communication Foundation (WCF)
- 'netTcpBinding'
- WCF duplex callbacks
- Multithreading
- Synchronisation locks
- Git 
- GitHub
- Microsoft Visual Studio

--- 

## Notes

- The server must be started before any client attempts to sign in.
- Application data is stored in memory only.
- Restarting the server resets the application data.
- PollingClient and DuplexClient can operate together. 
- Multiple instances of both clients can be used simultaneously.
