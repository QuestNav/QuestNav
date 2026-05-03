---
title: Choosing an Ethernet Adapter
---
# Choosing an Ethernet Adapter

The Quest 3 / 3S has only a USB-C port, so a USB-C to Ethernet adapter is required to connect to the robot's wired network.

:::danger
Not every USB-C to Ethernet adapter works with the Quest. The Quest's Android build only includes drivers for specific Ethernet chipsets. Community testing has shown that adapters using **Realtek RTL8153** or **ASIX AX88179** chips have the highest success rate. Adapters built around other chipsets, or knock-offs that misreport which chipset they use, often won't enumerate and are useless for QuestNav.
:::

:::info
Adapters not on the lists below may also work, but Quest compatibility isn't guaranteed by chipset alone. The Quest 3 / 3S use a customized Android build, so an adapter that works on a Linux PC or other Android device may still fail to enumerate on a Quest. The only reliable confirmation is another team testing the same model on a Quest 3 / 3S. If you've tested an unlisted adapter on a Quest, please contribute your results via [Contributing](#contributing) at the bottom of this page.
:::

## Recommended Adapters

The list below is ordered by preference. We highly recommend an adapter with **power passthrough** so the headset stays charged during matches, although a non-passthrough adapter paired with a [Zinc-V regulated 5V converter](./wiring#alternative-robot-powered-via-a-regulated-5v-converter) is also a perfectly valid setup.

| Name                                                                                     | Type                      | Status                              | Link                                                                                                    | Picture                                                                             |
|------------------------------------------------------------------------------------------|---------------------------|-------------------------------------|---------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|
| Cable Matters 5-Port USB-C Hub On-The-Go                                                 | USB-C to Ethernet + Power | Tested, Working, Highly Recommended | [Amazon](https://a.co/d/72hhWqz)                                                                        | <img src="/img/adapter-pictures/cable-matters-5-in-1.webp" width="500"/>            |
| onn. 8-in-1 USB-C Adapter, USB 3.0 and 4K HDMI Compatible                                | USB-C to Ethernet + Power | Tested, Working, Recommended        | [Walmart](https://www.walmart.com/ip/onn-8-in-1-USB-C-Adapter-USB-3-0-and-4K-HDMI-Compatible/590617670) | <img src="/img/adapter-pictures/onn-8-in-1.webp" width="500"/>                      |
| Belkin Connect USB-C to Ethernet + Charge Adapter 100W                                   | USB-C to Ethernet + Power | Tested, Working                     | [Amazon](https://a.co/d/grTsVxG)                                                                        | <img src="/img/adapter-pictures/belkin-connect-passthrough.webp" width="500"/>      |
| StarTech.com USB-C to Gigabit Ethernet Adapter - White - Thunderbolt 3 / 4 Compatible    | USB-C to Ethernet         | Tested, Working                     | [Amazon](https://a.co/d/aBIJCgZ)                                                                        | <img src="/img/adapter-pictures/star-tech-usb-c-to-ethernet-tb4.webp" width="500"/> |
| Cable Matters USB-C to Gigabit Ethernet Network Adapter                                  | USB-C to Ethernet         | Tested, Working                     | [Amazon](https://a.co/d/hBC5Hfr)                                                                        | <img src="/img/adapter-pictures/cable-matters-usb-c-to-ethernet.webp" width="500"/> |
| Belkin USB-C to Gigabit Ethernet Network Adapter                                         | USB-C to Ethernet         | Tested, Working                     | [Amazon](https://a.co/d/11adViN)                                                                        | <img src="/img/adapter-pictures/belkin-usb-c-to-ethernet.webp" width="500"/>        |

:::tip
The Cable Matters 5-Port USB-C Hub is our top recommendation: Ethernet, power passthrough, and well-tested under FRC competition conditions.
:::

## Known Non-Working Adapters

| Name                                                                                | Type              | Status                            | Link                             | Picture                                                                          |
|-------------------------------------------------------------------------------------|-------------------|-----------------------------------|----------------------------------|----------------------------------------------------------------------------------|
| Anker USB C to Ethernet Adapter, PowerExpand USB C to Gigabit Ethernet Adapter      | USB-C to Ethernet | Does Not Work. Will Not Enumerate | [Amazon](https://a.co/d/4fOaj1R) | <img src="/img/adapter-pictures/anker-powerexpand.webp" width="500"/>            |
| UGREEN USB C to Ethernet Adapter, Gigabit RJ45 to USB 3.0 Type-C (Thunderbolt 3)    | USB-C to Ethernet | Does Not Work. Will Not Enumerate | [Amazon](https://a.co/d/cvQbDFy) | <img src="/img/adapter-pictures/ugreen-usb-c-to-ethernet.webp" width="500"/>     |
| StarTech.com USB-C to Ethernet Adapter, USB 3.0 to Gigabit Ethernet Network Adapter | USB-C to Ethernet | Does Not Work. Will Not Enumerate | [Amazon](https://a.co/d/icEjAsG) | <img src="/img/adapter-pictures/star-tech-usb-c-to-ethernet.webp" width="500"/>  |

:::danger
The adapters listed above have been tested and confirmed NOT to work with Quest headsets. Do not purchase these for QuestNav, even if they appear similar to working models.
:::

## Alternative Solutions

**USB Splitters with Ethernet Adapters**

:::note
Some teams have had success using USB splitters like [this one](https://a.co/d/42pHOMf) with a USB-C to Ethernet dongle. However, vibration appears to make this approach less reliable over time. Your mileage may vary.
:::

## Testing Your Adapter

After purchasing an adapter, we recommend testing it thoroughly before competition:

1. Connect the adapter to the Quest headset
2. Connect an Ethernet cable from the adapter to your robot or development computer's network
3. Confirm the Quest receives a valid IP address: open `http://<Quest IP>:5801/api/status` from another device on the same network. If it returns valid JSON, the adapter is enumerating correctly.
4. **Stress test under realistic conditions**: mount the headset on a running robot and drive for at least 5 minutes (turns, accelerations, impacts). Watch `/api/status` over time and confirm `networkConnected` stays `true` — flips indicate the adapter is dropping.

:::tip
Always bring a backup adapter to competitions. Hardware failures often happen at the worst possible times.
:::

## Next Steps

Once you've selected an adapter, proceed to [Headset Setup](./device-setup) to configure your Quest for QuestNav.

## Contributing

Tested an adapter that's not on either list? Contribute it:

- Click the **Edit this page** button at the bottom of this page to edit it via GitHub, or
- Open a pull request against [QuestNav/QuestNav](https://github.com/QuestNav/QuestNav) directly.

Include the model name, your test results, a purchase link, and ideally a picture.
