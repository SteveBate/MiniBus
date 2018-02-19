# MiniBus

A small .Net messaging library built on top of MSMQ for application integration scenarios. Support for transactions, automatic retries, json serialization, and more. Provides a simple and reliable way of integrating applications and services via message queues minus the complexity of a full-on ESB. 

* NOTE - MiniBus now appears as mini.bus in nuget package manager

## Features

MiniBus offers the following features:

* Send to one or more queues (local and/or remote)
* Read messages synchronously or asynchronously
* Choice of XML or JSON serialization
* Automatic Message Distribution for load balancing
* Enlist in ambient transactions
* Configurable, robust automatic retries
* Move to error queue on failure
* Automatically create local queues
* Simple logging support
* Return error messages back to the read queue
* Copy message to one or more queues
* Fail fast option
* Discard failures

## Update

The core of MiniBus has now been rewritten to both more maintainable, testable, and reliable.

## How to use

A bus instance is created via the **BusBuilder** class using the many options available to configure it to your needs but first create a type to use as a message:

```csharp
public class PlaceOrder
{
	public string OrderNumber { get; set; }
	public string Description { get; set; }
}
```
#### Sending 

```csharp
// create a bus for sending messages
IBus bus = new BusBuilder()
    .WithLogging(new FileLogger())
    .DefineErrorQueue("MiniBus.errors")
    .DefineWriteQueue("MiniBus.messages1")
    .DefineWriteQueue("MiniBus.messages2@remotepc")
    .DefineWriteQueue("MiniBus.messages3")
    .CreateLocalQueuesAutomatically()
    .EnlistInAmbientTransactions()
    .AutoDistributeOnSend()
    .JsonSerialization()
    .TimeToBeReceived(TimeSpan.FromMinutes(1))
    .CreateBus();
	
// create your message type
var order = new PlaceOrder { o.OrderNumber = "N0232344", Description = "Fries and a Shake" };

// send it
bus.Send(order);
```

####	 Receving

Create a message handler by implementing the interface **IHandleMessage&lt;T&gt;**:

```csharp
class OrderHandler : IHandleMessage<PlaceOrder>
{
    public void Handle(PlaceOrder o)
    {
        // process the message
    }
}
```
Then register it with the bus and call the **Receive** method for synchronous processing:

```csharp
// create a bus for receiving messages

int slide = 4000; // time in milliseconds between retries multiplied by the current retry

IBus bus = new BusBuilder()
    .WithLogging(new FileLogger())
    .DefineErrorQueue("MiniBus.errors")
    .DefineReadQueue("MiniBus.messages1")
    .JsonSerialization()
	.NumberOfRetries(3, slide)
    .CreateBus();
	
// register one or more message handlers
bus.RegisterHandler<PlaceOrder>(new OrderHandler());

// process messages on the read queue synchronously
bus.Receive<PlaceOrder>();
```
Or **ReceiveAsync** for asynchronous processing:

```csharp
// register one or more message handlers
bus.RegisterHandler<PlaceOrder>(new OrderHandler());

// process messages on the read queue asynchronously
bus.ReceiveAsync<PlaceOrder>();
```
		
#### BusBuilder Options

The BusBuilder class is where we do all the configuration of our bus. We then call the **CreateBus** method to get back an instance we can use to begin sending or receiving messages.

##### * WithLogging

Unlike most libraries out there, I didn't want to force Log4Net or any other logging library on to the user because of 
<br/>
a) versioning issues 
<br/>
b) keeping MiniBus small, lightweight, and simple. 

MiniBus simply provides an interface called **ILogMessages**. The consuming application is responsible for providing an implementation of this interface to wrap around the logging library it is already using which in a console application could be as simple as logging to the console window:

```csharp
// log to the console
class ConsoleLogger : ILogMessages
{
    public void Log(string message)
    {
        Console.WriteLine(message);
    }
}
```

or alternatively a logging library such as Log4Net:

```csharp
// log with Log4Net
class FileLogger : ILogMessages
{
    public void Log(string message)
    {
        if(log.IsDebugEnabled)
            log.Debug(message);
    }

    static readonly ILog log = LogManager.GetLogger(typeof(Program));
}
```

##### * DefineErrorQueue

When a message cannot be processed, you don't want to lose it. By defining an error queue MiniBus has somewhere to place the failed message until you're ready to try again. In order to do so, you can create the bus and call <strong>ReturnAllErrorMessages</strong> to have the messages moved back to the read queue.

##### * DefineReadQueue

When creating the bus in the receiving application, you need to tell MiniBus which queue to read the messages from. DefineReadQueue does just that.

##### * DefineWriteQueue

In the sending application, you can tell MiniBus which queue(s) to send the message to via the DefineWriteQueue method. Unlike DefineErrorQueue and DefineReadQueue, DefineWriteQueue can be called many times. MiniBus will send a message to each queue that you declare with the DefineWriteQueue method.

##### * DefineWriteQueues

An alternative that allows all write queues to be defined in one call.

##### * CreateLocalQueuesAutomatically

Queues can be on the same machine as the application or on a remote machine. Remote queues are declared in each of the Define&lt;whatever&gt;Queue methods by using the @ syntax. For example <strong>myqueue@remotepc</strong>. Local queues do not include the @ symbol or machine name. If you declare local queues and they do not exist, calling the CreateLocalQueuesAutomatically method will ensure the queues are created for you before you use the bus to send or receive messages.

##### * JsonSerialization

By default, MiniBus serializes your messages as Xml. If you would rather use JSON then call this method on the BusBuilder object. When sending a message using XML serialization you'll need to share the message type between the sending and receiving applications as the type is embedded in the XML body as the root element. However, JSON serialization is much looser. There is no requirement to share the type between both endpoints. Deserialization of a message using JSON only looks for matching members between the data in the message body and the type you specify as the target for the deserialization process. Basically this means you can just write a seperate class in each application that look the same (or similar) for both serialization and deserialization without needing to share a dll containing a type.

##### * EnlistInAmbientTransactions

Sometimes you only want a message to be sent or received as part of a larger transaction, for example, if you are also inserting/updating a row in a database. This option ensures that the operations are treated atomically. In other words, when sending, the message will only be sent if the database operation succeeds. Conversely, when receiving, the message will only be removed if the database operation succeeds. A failure of the database during a read will cause the message to be moved to the error queue.

##### * NumberOfRetries

MiniBus by default will move a failed message to the error queue as soon as an error is detected. Sometimes, perhaps due to network latency in a web service call, the operation would succeed if tried again. The NumberOfRetries method let's you specify how many times MiniBus should retry the operation before giving up and moving the message to the error queue. Additionally, a new optional parameter, slidingRetryInterval has been added to allow a period of time to pass before a retry should occur.

##### * AutoDistributeOnSend

By default, when sending messages MiniBus will send the same message to all the writeQueues you have defined. Sometimes though you may want to evenly distribute messages bewteen all the queues in order to achieve load balancing with the receivers. Setting this option will cause MiniBus to send a message to a different queue on each successive call to Send.

##### * FailFast

Ordinarily when an error occurs processing a message it is moved to the designated error queue for later intervention. In most cases this is fine but sometimes it can be critical that message order is preserved. In other words, we don't want any subsequent messages to be processed until the failed message is fixed. The FailFast option causes the queue to immediately stop processing messages in the event of an error. The failing message itself stays on the read queue rather than being moved to the error queue so that when the issue is fixed it will be the first message to be processed.

##### * DiscardFailedMessages

Sometimes if a message fails you just don't care. Either it wasn't important or you know the same message will show up again soon, perhaps from a process that resends until a given state is determined. In this case setting the DiscardFailedMessages option will not put the failed message on the error queue, instead it will just throw it away leaving the error queue maintenance free.

##### * OnErrorAsync

Provides a hook to allow you to specify code that should be executed (asynchronously) if a message is placed on the error queue. Useful, for instance, for sending emails to notify an administrator.

##### * AutoPurgeSystemJournal

Left unattended and with no administrator monitoring MSMQ can grind to a halt with an "insufficient resources" error. This occurs when the system32\msmq\storage directoy has grown too large with copies of each and every message placed on the "Journal Messages" system queue. Using the AutoPurgeSystemJournal option tells MiniBus to attempt to clean up this queue every fifteen minutes in order to reduce the chances of the error occuring.

##### * UseJournal

Specifies whether a copy of the message should be stored on the originating PC

##### * TimeToBeReceived

Specifies how long a message should exist on the queue for before automatically being removed and disposed

## Building the Source

If you want to build the source, clone the repository, and open up MiniBus.sln.

```csharp
git clone https://github.com/SteveBate/MiniBus.git
explorer path/to/MiniBus/MiniBus.sln
```

## Supported Platforms
MiniBus targets .NET 4.0 and with regard to MSMQ, Windows 7, 8, Server 2008 and above

## License
[MIT License](http://opensource.org/licenses/MIT)

## Questions?
Feel free to submit an issue on the repository, or find me at [@Steve_Bate](http://twitter.com/Steve_Bate)
