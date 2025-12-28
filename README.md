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

The remaining projects, [QRiskTreeTest](https://github.com/simonec73/QRiskTree/tree/main/QRiskTreeTest) and [QRiskTreeParallelTest](https://github.com/simonec73/QRiskTree/tree/main/QRiskTreeParallelTest), are test applications respectively used to verify the main functionalities implemented by QRiskTree.Engine and how well it can execute multiple simulations and optimizations at the same time.

## How can I use QRiskTree?

The most immediate way to use QRiskTree is to simply use QRiskTreeEditor, but this is not the only way: QRiskTree.Engine is designed to be easily used in your applications. Here are some typical use cases.

### Interactive scripting

Do you want to use it to run scripts interactively? You can do that, for example using [LINQPad](https://www.linqpad.net/). Here is the recipe:

1. Open LINQPad and add QRiskTree.Engine.dll as a reference using **Reference and Properties** command from the **Query** menu. 
2. Insert the code you want to execute in the Query pane. You might take the code included in [Program.cs](https://github.com/simonec73/QRiskTree/blob/main/QRiskTreeTest/Program.cs) for testing purposes. You might need to make some simple adjustments, like removing the lines starting with **#region** and **#endregion**.
3. Change the **Language** to C# Statements and **.NET** to version 8.0.
4. Click on the **Run** button.

### Integrating within an application you own

Do you want to integrate it in your existing application? If it supports .NET 8.0, just add a reference to QRiskTree.Engine and you are good to go! If not, you might need to wrap it in some way, for example with a Web API.

You can also reuse some of the code provided with QRiskTreeEditor. For example, the View Models could be easily reused in a Web Application based on Blazor.

Starting from version 0.2.0 of the QRiskTree.Engine, you can host it in server applications shared among multiple users. This is because this version removes static variables which would have not allowed to configure the various models independently.

### Modifying it to fit your specific requirements

You are welcome to fork QRiskTree and make the changes that are required for you. Do you need to comply with a specific Quantitative Risk Analysis standard? Or integrate it with some Risk Management tool is in use by your Company? Feel free to do so. If you feel like the changes you have made might be of interest for the QRiskTree users community, please provide them to the main repo with a Pull Request (but I cannot guarantee they will be actually included), otherwise you are welcome to keep them for your own.

QRiskTree has been designed to be well structured and easily understandable. At least, I hope that it will be easily understandable. It currently lack part of the comments, but that's a priority for the future.

## What are the requirements for QRiskTree Editor?

QRiskTree Editor requires a computer with Windows 10 x64 or Windows 11 with at least 8GBytes of RAM.
The recommendation is to use QRiskTree Editor on computers with a fast multi-core CPU and at least 16GBytes or RAM.

QRiskTree Editor 0.3.0 on a computer based on AMD Ryzen 9 8945HS is able to optimize the [Big Model reference model](./Samples/Big%20model.json) with 10 risks defined at the Loss Event Frequency and Loss Magnitude level, and the same 10 mitigations assigned to every threat in about 2 minutes. The previous version, 0.2.0, took over 30 minutes for the same job. During the execution, the process has used around 350 to 550 MBytes of RAM (the previous version took around 250MBytes of RAM) and has fully used a little more than one core (around 7-8% of the CPU). 

With version 0.5.0, I have introduced the possibility to execute the optimization on multiple threads. This is ideal for execution on client-side applications like QRiskTree Editor. The code is designed to use only a portion of the CPU, ensuring that at least 20% is available for other activities. The execution of the Big Model now uses up to 65% of the CPU, which has 16 logical processors, to complete the optimization in a little over 12 seconds, about ten times less than 0.3.0 and 150 times less than 0.2.0. Memory usage has increased, with peaks of over 2GBytes.

> Note: the original Big Model didn't have Operational costs. There is a [new version of the Big Model]((./Samples/Big%20model%20(new).json) which addresses the problem. This new version increases the execution time to 15 seconds.

## Do you have some guidance on how to use QRiskTree Editor?

Please watch this video to get a quick introduction to QRiskTree Editor.
[![Welcome to QRiskTree Editor!](https://img.youtube.com/vi/u9vN_SIq5KY/maxresdefault.jpg)](https://youtu.be/u9vN_SIq5KY)

This guidance applies to version 0.2.0 and is still valid for version 0.3.0. With version 0.4.0, I've introduced some key changes, which expand QRiskTree Editor's capabilities. You can find their detailed description below. The prefix ([0.4]) specifies when the feature has been introduced. Version 0.5.0 has not introduced any new feature.

I'm planning to eventually extend this list in the future.

### [0.4] Control Types

It is now possible to specify the Control Type for Mitigations. There is a new property, called **Control Type** which can be edited in the **Mitigations** panel both in the table and in the properties. You can specify the following values. Preventive, Detective,  Corrective and Other. 

| Control Type | Description                                                  |
| ------------ | ------------------------------------------------------------ |
| Unknown      | This is the default value. It means that the Control Type has not been assigned. |
| Preventive   | Preventive controls reduce the probability and/or the impact of the risk.<br />They are also known as Preventative. |
| Detective    | Detective controls are used to detect attack while they are in progress.<br />As such, Detective controls have no direct effect on the potential losses, but they act as triggers for Corrective controls. See [[0.4] Auxiliary Mitigations](#[0.4]-auxiliary-mitigations) for understanding how you should manage them. |
| Corrective   | Corrective controls have the intent of blocking attacks while they are in progress, to limit the potential losses. They are usually triggered by Detective controls, and include automated actions and manual processes, like those that are started by an email sent by some Alert.<br />They are also known as Responsive. |
| Recovery     | Recovery controls are used to recover the system to its original state after a compromise. |
| Other        | This category is used to represent any other category of controls not included in the list. For example, a purely Deterrent control would be assigned to this category. |

Control Types are purely informative. Assigning a specific Control Type doesn't have any effect on the simulations.

Finally, you can only set the Control Type in the Mitigations panel. You cannot set it on the Mitigations shown in any other panel.

### [0.4] Auxiliary Mitigations

Some controls have no direct effect on the residual risk. For example, purely Detective controls will only alert you for attacks in progress, but will do nothing about that. They are enablers for other controls, namely the Corrective ones. As such, they represent just a cost, as the effectiveness is given by the controls they enable.

This is where the new Auxiliary flag comes handy. If you mark a Mitigation assigned to a Risk as Auxiliary, using the specific check box shown in the window below, its effectiveness is not considered in the simulation.

![The Auxiliary setting.](./Pictures/Auxiliary.png)

Auxiliary mitigations are always considered as implemented. Therefore, they always affect the implementation and operation cost. This is because otherwise the optimization mechanism would never include them because they are not effective and only represent a cost.

You are not required to provide an estimation for the effectiveness of Auxiliary mitigations, as it is ignored. This means that you will not receive an error during the validation that is performed before the optimization activities.

#### Consequences of the Auxiliary Mitigations

The chosen implementation of the Auxiliary Mitigations has a couple of attention points you should be aware of.

First, the Auxiliary flag is applied to the Mitigations applied to Risks, not to the Mitigations themselves. This means that you might decide that for some Risk, a Mitigation is auxiliary to some other Mitigation, while for other Risks it is not. The effectiveness will be ignored only when the Auxiliary flag is set. Moreover, a Mitigation that has the Auxiliary flag set for some Risk will be always included in the optimized set, even if that flag is not set for that Mitigation everywhere.

The second consequence is that a Mitigation that is flagged as Auxiliary for some Mitigation, will be always included even if it is not necessary. This might create a situation like the following example:

- You have three Mitigations: a preventive control, P, a detective control, D, and a corrective control, C. 
- D is auxiliary for C. 
- The optimization identifies P and D as the optimal mitigations. 
- Given that D is marked as auxiliary, it is always included, even when it's not necessary. 
- You know that D is auxiliary for C, which is not included in the optimal list. Therefore, D is not necessary and the optimal mitigation is just P. 
- The estimated loss will be lower than the range calculated by the optimization. If you want to estimate the total cost range with only P, you can disable D and C in the Mitigations panel, and then repeat the optimization. There is no need to repeat the calculation of the baseline.

You might wonder why the Auxiliary flag is implemented as such, instead of specifying the Mitigations towards which your Mitigation is Auxiliary. This is because that approach would complicate the model and the user interface, without improving significantly what QRiskTree can do. It's an arbitrary decision, and you might want to propose a different implementation. Feel free to [contribute](#how-can-i-contribute) with your code, if you come up with a smart way to do so without impacting too much the solution.

### [0.4] Currency Symbol and Monetary Scale

Version 0.4 introduces the possibility to specify a different currency than the one set for your OS. It also allows you to specify the Monetary Scale, which can be empty or represent the scale like K for thousands, M for millions, B for billions and so on. All currency numbers are represented based on these parameters.

For example, with the configuration shown below, two hundred thousands will be represented as **€200,000K** on systems having comma as the thousands separator.

![Currency Symbol and Monetary Scale](./Pictures/Properties.png)

Changes to Currency Symbol and Monetary Scale are immediately effective and their value is stored in the model file. Please check the [Auxiliary example](./Samples/Auxiliary.json).

### [0.4] Charts

QRiskTree Editor now is able to create four charts as a result of the Calculations. The first shows the distribution of the Baseline, the second and third show the distribution of the combined costs for the optimal combination of mitigations related to respectively to the first and following years. These three charts are very similar to the following picture.

![The distribution of the Baseline](./Pictures/Baseline.png)

You can already notice that the chart shows the distribution of the samples, the percentile curve, highlighting the min and max percentiles chosen in the Properties (in this case, they are respectively the 10th and the 90th percentile). The left axis shows how many samples have been generated over the whole population (in this case, 100000). The right axis applies instead to the percentile curve, and shows a scale from 0 to 100. In the bottom axis, you can find the scale for the overall cost. You can get a lot of information from this chart: for example, you can determine that there is a 90% probability that the losses will be less than €86.4M for the system under scrutiny. Assuming that this chart refers to the Baseline, this number and the distribution give a very clear visual representation of the potential cost of the system considering the residual risk, if we do nothing.

The fourth and last chart, called **Comparison** provides a quick representation of the impact of the optimal mitigations on the total cost for security, which as already discussed includes the residual risk, and the implementation and operation costs for the mitigations.

![The Comparison chart.](./Pictures/Comparison.png)

The chart shows quite effectively how much you can save by implementing the selected mitigations. If the saving here doesn't look like much, you must consider that this includes the implementation and operation costs for the mitigations.

Other models might produce even better results. Consider for example the [Big Model reference model](./Samples/Big%20model.json). This is what you can get by optimizing it. Isn't that convincing?

![The Comparison chart for the Big Model](./Pictures/BigModelComparison.png)

This Comparison chart is slightly different from the other charts. First, the left and right axis apply respectively to the baseline distribution and to the optimized distribution. It is important to separate them, otherwise the chart might not be readable: given that each distribution is obtained with the same number of samples, the optimized one tends to be much taller as it is more compressed. We also have a legend, which is missing from the other charts, to ensure you can quickly determine the meaning of each distribution.

#### How to use the charts

The charts created by QRiskTree Editor are interactive. For example, you can use your mouse to move the chart, zoom or change some axis' scale. 

- Use the right mouse button to move the chart or the axis.
- Use the left mouse button on the chart to show the exact numbers related to the selected point.
- Use the mouse wheel, to change the scale.
- Use the mouse wheel button to zoom to an area of the chart.

That said, whatever change you do, it is most important to ensure that the two histograms in the Comparison chart have approximately the same height, otherwise you might provide a wrong representation of the reality, like that the optimized situation is worse than the baseline, which is typically wrong.

#### Using the charts in other tools

QRiskTree Editor does not include the ability to export the charts as images. If you need to export one of the charts and reuse in some tool, like PowerPoint, you can use the Snipping tool that is available with Windows 11 by clicking ⊞+SHIFT+S and then selecting the area to be copied. As an alternative, you can search for "Snipping" in the Start menu or using ⊞+R and then typing "snippingtool", both without the quotes.

## What are the improvements to QRiskTree Editor?

QRiskTree Editor has already had a few versions. Here are the key improvements for the most important releases, from the latest to the first.

- **0.5.0**: Parallelization for Clients.
  - Introduced the possibility to execute the Optimization process on multiple threads. This is useful when the optimization is run on some Client-side application, like QRiskTree Editor. By default, optimizations use up to 80% of the CPU, more typically around 60%. This, combined with other optimizations implemented with this version, has allowed to improve the optimization time significantly, around 10 times better than the previous version.
  - Automatic calculation of the Confidence.
  - Various optimizations and bug fixes.
- **0.4.0**: First major features improvement.
  - Improved Mitigations management: Control Types and Auxiliary Mitigations.
  - Added the possibility to customize the currency: Currency Symbol and Monetary Scale.
  - Added Charts to improve the readability of the calculation results.
- **0.3.0**: Intelligent Caching.
  - Major performance improvement. The [Big Model reference model](./Samples/Big%20model.json) is now about 15 times faster.
  - Improved consistency of the results.
- **0.2.0**: Parallel Execution for Servers.
  - Added support for parallel execution on Servers.
  - Corrected the optimal ranges shown by the tool.
- **0.1.1**: Fixed minor bugs.
- **0.1.0**: First public version.

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
