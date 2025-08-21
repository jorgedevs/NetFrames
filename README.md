![NetFrames GitHub Banner](/Assets/Images/jorgedevs-netframes.jpg)

# NetFrames

NetFrames is a lightweight, connected display platform that allows digital picture frames or embedded screens to automatically fetch and display images from a central server. Designed for Raspberry Pi, Meadow, or mobile clients, NetFrames is ideal for personal photo galleries, public displays, or IoT art installations.

## Contents

* [Architecture](#architecture)
* [Build and Setup](#build-and-setup)
* [Support](#support)

## Architecture

![NetFrames.WebPortal](Assets/Images/netframes-software-stack.png)

### [NetFrames.Server](/Source/NetFrames.Server/)

NetFrames.Server is a .NET Core Web API, exposing endpoints to:
* Upload an image
* Get list of images
* Get specific image by ID.

### [NetFrames.WebPortal](/Source/NetFrames.WebPortal/)

The purpose of this portal is to manage the image collection that client devices will display on its screens.

![NetFrames.WebPortal](Assets/Images/netframes-portal.png)

### [NetFrames.EmbeddedClient](/Source/NetFrames.EmbeddedClient/)

Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.

#### Wiring

You can wire a Meadow F7 Feather board with an ILI9341 like the diagram below:

![wiring netframes with a Meadow board](Assets/Images/netframes-wiring.jpg)


#### Enclosure

Feel free to 3D print this enclosure so you can place it on a desk or mount it on a wall. STL files are [here](/Assets/Enclosure/) or download directly from [TinkerCad](https://www.tinkercad.com/things/222cHvoUr3W-netframes-case).

![NetFrames.EmbeddedClient](Assets/Images/netframes-enclosure.jpg)


## Build and Setup

Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.

## Roadmap

The following table show's whats available and what features are next in upcoming updates.

## Support

Finding bugs or wierd behaviors? File an [issue](https://github.com/jorgedevs/NetFrames/issues) with repro steps.
