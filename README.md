# .net-client

## CI status

<img src=https://ci.appveyor.com/api/projects/status/github/splitio/.net-client?branch=master&svg=true&passingText=master%20-%20ok&pendingText=master%20-%20running&failingText=master%20-%20failing>

<img src=https://ci.appveyor.com/api/projects/status/github/splitio/.net-client?branch=development&svg=true&passingText=development%20-%20ok&pendingText=development%20-%20running&failingText=development%20-%20failing>


## Installing Split SDK using Nuget

To install Splitio, run the following command in the Package Manager Console

```
Install-Package Splitio
```

## Write your code!

SDK Configuration options

```cs
    var configurations = new ConfigurationOptions();
    configurations.FeaturesRefreshRate = 30;
    configurations.SegmentsRefreshRate = 30;

```

Create the Split Client instance. 

```cs
var factory = new SplitFactory("API_KEY", configurations);
var sdk = factory.Client();
```

Checking if the key belongs to treatment 'on' in sample_feature. 

```cs
if (sdk.GetTreatment("key", "sample_feature") == "on") 
{
    //Code for enabled feature
} 
else 
{
    //Code for disabled feature
}
```

Using matching key and bucketing key

```cs
Key key = new Key("sample_matching_key", "sample_bucketing_key");

if (sdk.GetTreatment(key, "sample_feature") == "on") 
{
    //Code for enabled feature
} 
else 
{
    //Code for disabled feature
}
```

### Using Attributes in SDK

In order to Target Based on Custom Attributes, the SDK should be given an attributes dictionary. In the example below, we are rolling out a feature to users. The provided attributes - plan_type, registered_date, and deal_size - can be used in the Split Editor to decide whether to show the 'on' or 'off' treatment to this account.

```cs
var attributes = new Dictionary<string, object>();
attributes.Add("plan_type", "growth");
attributes.Add("registered_date", System.DateTime.UtcNow);
attributes.Add("deal_size", 1000);

string treatment = sdk.getTreatment("CUSTOMER_ID", "FEATURE_NAME", attributes);

if (treatment == "on") 
{
    // insert on code here
} 
else if (treatment == "off") 
{
    // insert off code here
} 
else 
{
    // insert control code here
}
```

In the attributes Dictionary used in this example, you can provide three types of attributes:

*String Literal Attributes*

String literal attributes capture the concept of a character Strings. The values for String literal attributes should be of type String. For instance, the value for attribute plan_type is a String. Such attributes can be used with the following matchers:

    in list

*Numeric Attributes*

Numeric attributes capture the concept of signed numbers. Their values can be of type long or int. For instance, the value for attribute deal_size is int. Negative numbers are allowed. Floating point numbers are not supported. Numeric attributes can be used with the following matchers:

    is =
    is >=
    is <=
    is between

*DateTime Attributes*

DateTime attributes capture the concept of a Date, with optional Time. DateTime should be expressed in UTC.

DateTime attributes can be used with the following matchers:

    is on
    is on or after
    is on or before
    is between

### FAQ On Attributes

*What happens if a Split uses an attribute whose value is not provided in code?*

For instance, take this Split definition:

if user.age <= 60 then split 100%:on
else
if user is in segment all then split 100%:off

If the value for age attribute is not provided in the attributes map passed to getTreatment call, then the matcher in the first condition user.age <= 60 will not match. The user will get the evaluation of the second condition: if user is in segment all then split 100%:off which is off.

*What happens if a Split uses an attribute whose value in code is not of correct type?*

For instance, take this Split definition:

if user.plan_type is in list ["basic"] then split 100%:on
else
if user is in segment all then split 100%:off

And say the value provided for plan_type is an int instead of a String:

In this scenario, the matcher in the first condition user.plan_type is in list ["basic"] will not match. The user will get the evaluation of the second condition: if user is in segment all then split 100%:off which is off.

*How can I use boolean types?*

At this point, the SDK does not provide matchers specifically for booleans. You can either translate booleans into a numeric attribute 1 or 0 and use is = 1 matcher, or a String attribute true or false and use is in list ["true"] matcher.

*How does the SDK perform date equality?*

If we have the following Split:

if user.registration_date is on 2016/01/01 then split 100%:on

Then there are 8.64 x 10^7 milliseconds that can fall on that date. The SDK ensures that if you provide any of those milliseconds, it will be considered equal to 2016/01/01.
    

### Advanced Configuration of the SDK 

The SDK can configured for performance. Each configuration has a default, however, you can provide an override value while instantiating the SDK.


```cs    
    var configurations = new ConfigurationOptions();
    configurations.FeaturesRefreshRate = 30;
    configurations.SegmentsRefreshRate = 30;
    configurations.ImpressionsRefreshRate = 30;
    configurations.MetricsRefreshRate = 30;
    configurations.ReadTimeout = 15000;
    configurations.ConnectionTimeOut = 15000;
    
    var factory = new SplitFactory("API_KEY", configurations);
    var sdk = factory.Client();
```

###  Blocking the SDK Until It Is Ready 

When the SDK is instantiated, it kicks off background tasks to update an in-memory cache with small amounts of data fetched from Split servers. This process can take up to a few hundred milliseconds, depending on the size of data. While the SDK is in this intermediate state, if it is asked to evaluate which treatment to show to a customer for a specific feature, it may not have data necessary to run the evaluation. In this circumstance, the SDK does not fail, rather it returns The Control Treatment.

If you would rather wait to send traffic till the SDK is ready, you can do that by blocking until the SDK is ready. This is best done as part of the startup sequence of your application. Here is an example:

```cs
        ISplitClient client = null;

        try
        {            
            var configurations = new ConfigurationOptions();
            configurations.Ready = 1000;
            var factory = new SplitFactory("API_KEY", configurations);
            client = factory.Client();
        }
        catch (TimeoutException t)
        {
            // SDK was not ready in 1000 miliseconds
        }
```

###  Running the SDK in 'off-the-grid' Mode 

Features start their life on one developer's machine. A developer should be able to put a feature behind Split on their development machine without the SDK requiring network connectivity. To achieve this, Split SDK can be started in 'localhost' (aka off-the-grid mode). In this mode, the SDK neither polls nor updates Split servers, rather it uses an in-memory data structure to determine what treatments to show to the logged in customer for each of the features. Here is how you can start the SDK in 'localhost' mode:

```cs
    var factory = new SplitFactory("localhost", configurations);
    var client = factory.Client();
```

In this mode, the SDK loads a mapping of feature name to treatment from a file at $HOME/.split. For a given feature, the specified treatment will be returned for every customer. In Split terms, the roll-out plan for that feature becomes:

```
if user is in segment all then split 100%:treatment
```

Any feature that is not mentioned in the file is assumed to not exist. The SDK returns The Control Treatment for every customer of that feature.

The format of this file is two columns separated by whitespace. The left column is the feature name, the right column is the treatment name. Here is a sample feature file:

```
# this is a comment

reporting_v2 on # sdk.getTreatment(*, reporting_v2) will return 'on'

double_writes_to_cassandra off

new-navigation v3
```

###  Split Manager 

In order to obtain a list of Split features available in the in-memory dataset used by Split client to evaluate treatments, use the Split Manager.

```cs
    var factory = new SplitFactory("API_KEY", configurations);
    var splitManager = factory.Manager();
```

Currently, SplitManager exposes the following interface:

```cs
    List<SplitView> Splits();
    SplitView Split(String featureName);
```

calling splitManager.Split(String featureName) will return the following structure:

```cs
    public class SplitView
    {
        public string name { get; set; }
        public string trafficType { get; set; }
        public bool killed { get; set; }
        public List<string> treatments { get; set; }
        public long changeNumber { get; set; }
    }
```

###  Randomization of Polling Periods 

The SDK polls Split servers for feature split and segment changes at regular periods. The configuration parameters FeaturesRefreshRate and SegmentsRefreshRate control these periods. Say the value set for FeaturesRefreshRate is 60 seconds. Then, instead of polling at exactly p seconds interval, each SDK instance polls at a randomly chosen time period in the range (0.5 * p, p). This randomization is done to avoid letting SDKs deployed across multiple machines to poll at the same time which can lead to bad performance.

###  Logging in the SDK 

The .Net SDK uses log4net for logging. You can configure it this way, before you create a SplitFactory instance:

```cs
    FileAppender fileAppender = new FileAppender();
    fileAppender.AppendToFile = true;
    fileAppender.LockingModel = new FileAppender.MinimalLock();
    fileAppender.File = @"ANY FILE NAME";
    PatternLayout pl = new PatternLayout();
    pl.ConversionPattern = "%date %level %logger - %message%newline";
    pl.ActivateOptions();
    fileAppender.Layout = pl;
    fileAppender.ActivateOptions();

    log4net.Config.BasicConfigurator.Configure(fileAppender);
    
    ...
    
    var factory = new SplitFactory("API_KEY", configurations);
```

Or you can configure it using all log4net available options. Learn more [here](https://logging.apache.org/log4net/release/manual/introduction.html)

In the previous example, we show you how to configure a FileAppender. Also you can create a custom appender that implements AppenderSkeleton, for example:

```cs
    public class MyCustomAppender : AppenderSkeleton
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            // Write your code here. For example:
            // MyLogService.Log(loggingEvent.Level.Name, loggingEvent.MessageObject);
        }
    }
```
Where MyLogService could be whatever you want.

And configure log4net to use it:

```cs
    MyCustomAppender appender = new MyCustomAppender();
    appender.ActivateOptions();

    log4net.Config.BasicConfigurator.Configure(appender);
    
    ...
    
    var factory = new SplitFactory("API_KEY", configurations);
```

If you don't configure log4net yourself, the SDK creates a default file appender and register logs in 'Logs\split-sdk.log' file.
 