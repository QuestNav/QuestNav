---
title: Testing & Simulation 
---

## Connecting to PC

Once your team number has been configured QuestNav will attempt to connect to a Network Tables server at 10.TE.AM.2. 
QuestNav can also be configured to use a specific IP address:
1. Open a browser to `http://<Quest IP Address>:5801`
2. Select the `General` tab
3. Enter the IP Address of the network table server in `Debug IP Override`
   
<img src="/img/web-interface/DebugIP.webp" width="800"/>

:::warning
This setting should NEVER be used during a competition. To reset, follow steps 1-2 above, then clear the contents of `Debug IP Override`.
:::

## [Python QuestNav Viewer](https://github.com/juchong/python-questnav-viewer)

A python desktop application with comprehensive GUI viewer for receiving and visualizing Quest headset tracking data.
This project also acts as sample code for interfacing with QuestNav using python.