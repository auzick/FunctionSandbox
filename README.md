# FunctionSandbox
At a recent lunch & learn, we spent some time examining Azure Durable Functions, which is an evolution of the Azure Function serverless computing model. Durable functions enable orchestration of activities, in a scalable, consumption-based model, that would otherwise have required an “always up” stateful platform. This is the technology that Azure uses to manage workflows under the hood of Logic Apps.

This code repo contains the demo project used for this L&L. This app simulates the process of a user registration, where the durable orchestrator manages a process of sending a verification email to the user-supplied address, and a verification SMS to the user-supplied phone number. This demonstrates function chaining, and the idea that an orchestrator can wait forever for those external events without consuming resource. It using Twilio to send SMS and SendGriud to send email. While not a fully-baked solution, it is fully functional, and also demonstrates using DI for services like SendGrid and Twilio.

## Don't bore me - I want to just jump in now.

You can clone the project, but you'll need to do some stuff to run it.

First, you need an Azure account, a Twilio account and a SendGrid account.

If you have a Visual Studio subscription, you most likely have 50 free bucks a months on an Azure account. If not, well, get one, or set up a paid Azure account.

You can get a free [Twilio trial account](https://www.twilio.com/try-twilio). It has a short expiration, but a standard pay-as-you-go account is pretty cheap.

SendGrid offers a [developer account](https://sendgrid.com/pricing/). I think you get about 100 emails a day; should be enough.

### Provisioning

I tried to make this easier by adding a "provisioning" console app project to this solution. This app will create and provision an Azure resource group, and can also update your local dev json files for local debugging. *(It uses the Azure Management Fluent API's and Azure REST API's to do this, if you're interested in how that works see* `FunctionSandbox.Provision\ProvisionAzure.cs` *)*

When you run this app (`dotnet run --project FunctionSandbox.Provision/FunctionSandbox.Provision.csproj`), it will prompt you for these strings:

- **Resource group name:** the name of the resource group you want to create
- **Twilio account SID:** The SID of your twilio account
- **Twilio auth token:"** The auth token for your Twilio account([Docs](https://support.twilio.com/hc/en-us/articles/223136027-Auth-Tokens-and-How-to-Change-Them))
- **Twilio number for sending SMS:** A valid Twilio phone number that supports SMS
- **SendGrid API key:** The API key for your SendGrid account
- **Email from address:** The "from" address to use when sending emails
- **Write config files?:** Do you want the app yo update your appsettings.Development.config and local.settings.config files? 

If all goes to plan, the app will now create a new Azure resource group, populate it with all the resources you need (using an F1 free app service plan), and populate all the configuration settings on the WebApp and FunctionApp. Pre-built zip deploy package (created from this project's dev-ops pipeline) will be deployed to the function app and the web portals. If you cerate a different repo/pipeline, you can trigger azure-pipelines on check-in.

The app will save your answers to the file `provisionSettings.json` to supply defaults for next time, or you can just use the values in the file by using the `--quiet` switch. Note: The  **Write config files** preference is purposely not saved in your provisioning settings file. If running "quietly", supply the `--writeConfigs` switch to force it to re-write your config file.

If you ran it before you read this far, it already complained about credentails and told you what to do. If not, do this before you run it the first time:

Using the Powershell AZ module (or from the Azure CLI):
  1. Log in to correct Azure subscription azure (az login)
  2. Make sure you are in the correct subscription (az account show)
  3. Run this command: `az ad sp create-for-rbac --sdk-auth`
  4. Copy/paste the json response to the file 'azureauth.json' in the root of the FunctionSandbox.Provision directory.");

## Why durable functions?

\<history\>

Historically, Azure function applications have been architected used with HTTP and Queue triggers in order to “chain” functions together to create workflows. This leveraged the cost and scale benefits of scalable serverless computing, but often became unweildly. An application could have a large numbers of functions, each listening to a queue and perhaps writing a message to another queue to trigger another function. It required tracing through the code or maintaining Visio diagrams in order to understand how the application functioned, so to speak. 

\</history\>

Durable functions introduce the concept of “orchestrator” and “activity” functions. Orchestrators govern the workflow of a process, calling activity functions in turn, passing them data and reacting to data passed back. In addition, they can “wait” for external triggers (events) and use the event data to decide how to proceed. This gives us several big advantages:

- While “awaiting” for an activity to complete, or while “awaiting” an external event, or even while waiting for some time to elapse (i.e. Thread.Sleep()),  the orchetrator function is not consuming compute resources. Azure maintains a storage account with a set of tables and blobs, and stores “state” and “history” whenever an orchestrator awaits an asynchronous task. When the task returns, Azure rehydrates the function and resumes execution.

- When doing classic function chaining, there are limitations on the amount of data that can be passed through queue messages. This often leads to having to manage tables and blobs, and passing keys and pointers in the queue message. With a durable orchestration, Azure manages the queues and blobs. You just call an activity and pass in an object and declare the return type as normal. Azure manages serializing the inputs and outputs, and uses a combination of queues and blobs as it sees necessary depending on the object sizes.

- With classic queue-based function chaining, if a function throws an error, Azure retries it a few times and then moves the message to a “poison” queue. It’s up to the developer or administrator to figure out how to monitor poison queues and restart processes. With durable functions, the orchestrator can use “try/catch” around calls to activities. If the activity throes an error, it it is raised in the orchestrator for it to handle. This allows developers to make apps much more resilient, and do proper logging and data persistence to handle errors.

There are some constraints, of course. Orchestrators must be written deterministically, because they are “replayed” regularly, and values established before the resumption point cannot change fro execution to execution. That may feel restrictive, but remember that an orchestrator shouldn't do any “work”; it should wave the baton for activity functions that do the actual work. 

## Read more

There are a lot of other advantages and cool features that I didn't even get into. Microsoft has good documentation on concepts, patterns, and code examples:

- [Documentation home](https://docs.microsoft.com/en-us/azure/azure-functions/durable/)
- [Overview of development patterns](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview?tabs=csharp)
- [Code samples](https://docs.microsoft.com/en-us/azure/azure-functions/durable/)


