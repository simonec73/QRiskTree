# QRiskTree
*Bridging Security and Business Decisions*



QRiskTree, short for Quantitative Risk Tree, is a library and a tool designed to help organizations determine when security is enough and which mitigations offer the best Return on the Security Investment. It enables data-driven decisions by combining threat modeling with quantitative risk analysis.

QRiskTree has been designed to address a serious gap in current threat modeling practices, but it can be applied to many other contexts. For example, it might prove beneficial to you if you need to:

- Evaluate the potential losses related to multiple uncorrelated risks which might happen during the year.
- Determine the most effective strategy to address multiple risks.
- Optimize your security investments for the next year.

QRiskTree aligns with the ideas described in:

- [Improving Return on Security Investment: Evaluating the Current Risk - The Open Group Blog](https://blog.opengroup.org/2024/03/20/improving-return-on-security-investment-evaluating-the-current-risk/)
- [Open FAIR and the ROI for Threat Modeling](https://www.threatmodelingconnect.com/events/open-fair-and-the-roi-for-threat-modeling)
- [Using Quantitative Analysis with System Threat Modeling Adopting Open FAIR™ Analysis with Threat Modeling to Maximize Return on Security Investment](https://publications.opengroup.org/w245)

> **IMPORTANT**
> The work implements a very personal take on the ideas introduced with the above material. It does not implement any specific standard or risk management framework. If you decide to use it as a tool to support your Compliance requirements, it is your responsibility to ensure that QRiskTree fits those requirements and aligns with your standards of reference, by eventually making the necessary changes to the code.

This is a project developed by a single person as a passion project, and is shared with you with the intent of providing the Security and Business communities a better way to communicate with each other and collaborate in creating better, more secure products.

With this intent of allowing the widest adoption, I've decided to provide QRiskTree under [MIT license](https://github.com/simonec73/QRiskTree?tab=MIT-1-ov-file#readme). You can use it for any lawful purpose, for free, including creating commercial software.

## What is QRiskTree composed of?

QRiskTree is first and foremost a library you can use in your own applications: [QRiskTree.Engine](https://github.com/simonec73/QRiskTree/tree/main/QRiskTree.Engine). This contains the core logic and is designed to be used as part of other applications.

Another component is [QRiskTreeEditor](https://github.com/simonec73/QRiskTree/tree/main/QRiskTree.Editor), a simple Windows Desktop application exposing QRiskTree functionalities.

The final part is represented by [QRiskTreeTest](https://github.com/simonec73/QRiskTree/tree/main/QRiskTreeTest), a test application used to verify the main functionalities implemented by QRiskTree.Engine.

## How can I use QRiskTree?

The most immediate way to use QRiskTree is to simply use QRiskTreeEditor, but this is not the only way: QRiskTree.Engine is designed to be easily used in most applications. Here are some typical use cases.

### Interactive scripting

Do you want to use it to run scripts interactively? You can do that, for example using [LINQPad](https://www.linqpad.net/). Here is the receipt:

1. Open LINQPad and add QRiskTree.Engine.dll as a reference using **Reference and Properties** command from the **Query** menu. 
2. Insert the code you want to execute in the Query pane. You might take the code included in [Program.cs](https://github.com/simonec73/QRiskTree/blob/main/QRiskTreeTest/Program.cs) for testing purposes. You might need to make some simple adjustments, like removing the lines starting with **#region** and **#endregion**.
3. Change the **Language** to C# Statements and **.NET** to version 8.0.
4. Click on the **Run** button.

### Integrating within an application you own

Do you want to integrate it in your existing application? If it supports .NET 8.0, just add a reference to QRiskTree.Engine and you are good to go! If not, you might need to wrap it in some way, for example with a Web API.

You can also reuse some of the code provided with QRiskTreeEditor. For example, the View Models could be easily reused in a Web Application based on Blazor.

### Modifying it to fit your specific requirements

You are welcome to fork QRiskTree and make the changes that are required for you. Do you need to comply with a specific Quantitative Risk Analysis standard? Or integrate it with some Risk Management tool is in use by your Company? Feel free to do so. If you feel like the changes you have made might be of interest for the QRiskTree users community, please feel free to provide them to the main repo with a Pull Request (but I cannot guarantee they will be actually included), otherwise you are welcome to keep them for your own.

QRiskTree has been designed to be well structured and easily understandable. At least, I hope that it will be easily understandable. It currently lack comments, but that's a priority for the future.

## What are the requirements for QRiskTree Editor?

QRiskTree Editor requires a computer with Windows 10 x64 or Windows 11 and runs with minimal hardware. As of today, the QRiskTree library is single thread: this means that you might get better execution times with CPUs having better single-core performance.

A computer based on AMD Ryzen 9 8945HS is able to optimize a model with 10 risks defined at the Loss Event Frequency and Loss Magnitude level, and the same 10 mitigations assigned to every threat in over 30 minutes. During the execution, the process has consistently used around 250 MBytes of RAM and has fully used a little more than one core (around 7-8% of the CPU). As it is, the behavior is ideal for being incorporated in server applications serving multiple requests at the same time. Moreover, you can start an optimization job while you can safely continue to use your PC. For these reasons, I will not change it to support multi-threaded execution anytime soon.

## Do you have some guidance on how to use QRiskTree Editor?

Please watch this video to get a quick introduction to QRiskTree Editor.
[![Welcome to QRiskTree Editor!](https://img.youtube.com/vi/u9vN_SIq5KY/maxresdefault.jpg)](https://youtu.be/u9vN_SIq5KY)

## Can I customize QRiskTree for my purposes?

Absolutely! Please Fork the QRiskTree repo and use the code as you see fit.

QRiskTree is developed using .NET 8.0. QRiskTree Editor is based on WPF. Both rely on third-party components that are free, Open Source and licensed with the same MIT license.

You should use the latest release of [Visual Studio 2022](https://visualstudio.microsoft.com/). The free Community edition is enough.

## Is QRiskTree supported?

No. It is provided as-is, with no guarantees of any sort.

## Is QRiskTree maintained?

In a sense, you can expect I will continue to update it for a while, but there is no guarantee I will maintain it in the future. It all depends on how much I will be able to use it myself.

When you get it, it might be a good idea to ensure at least that all references are updated and eventually recompile it.

## Is QRiskTree secure?

I am an Application Security expert and I try to write solutions that are as secure as possible and with no obvious vulnerabilities. I am scanning the code using [Security Code Scan](https://security-code-scan.github.io/), a free tool for scanning .NET code. I also rely on GitHub Dependabot and on GitHub Copilot to help identify vulnerabilities. Still, there is the possibility that there is some undetected vulnerability in the components I use, or in the code I write. Please treat my code with care, as you would normally do with every code written by a third party. As explained above, I do not provide any guarantee with my code, including about its security.

That said, I'm planning to prepare a short document describing its security characteristics and potential security issues.

## How can I contribute?

If you have any issue with it, please use the [Issues](https://github.com/simonec73/QRiskTree/issues) page to share it with me. You can also provide comments and desiderata in the [Discussions](https://github.com/simonec73/QRiskTree/discussions) page. And, of course, you can also provide proposals for code changes via [Pull Requests](https://github.com/simonec73/QRiskTree/pulls). I cannot guarantee that any of that will be followed up, but there is a good chance I will.
