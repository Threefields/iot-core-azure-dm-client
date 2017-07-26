# Library Reference

The capabilities exposed by the **Windows IoT Azure DM Client Library** can be accessed through to main types of interfaces:
1. Through a direct .Net API.
2. Through Device Twin/Azure IoT Hub direct methods (with json as the underlying data format).

Here's a diagram of what this looks like:

<img src="dm-architecture-application-library.png"/>

## Direct Method Format

Some device management actions are initiated by direct methods. Such methods start with the `microsoft.management` prefix followed by the method name. The method payload (when non-empty) is in JSON format. The return payload (if not empty) is also in JSON format.

Example:

```
microsoft.management.transmogrify
```

The payload for a method can look as follows:
```
"parameter" : "value"
```

The exact specification for each device management operation is defined in the [Specification](#specification) below.

## Device Twin Format

Certain device management operations are initiated by desired properties set from the IoT Hub. For example, some configuration settings are set by the desired properties as depicted in the example below:

```
"desired": {
    "microsoft" : { 
        "management" : {
            "key1" : value1,
            "key2" : value2,
            ...
        }
    }
    ...
}
```

## Specification

The specification for each operation is provided below.

- [Application Management](application-management.md)
- [Certificates Management](certificate-management.md)
- [Device Information](device-info.md)
- [Device Factory Reset](device-factory-reset.md)
- [Device Health Attestation](device-health-attestation.md)
- [Diagnostics Logs](diagnostic-logs-management.md)
- [Reboot Management](reboot-management.md)
- [Time Management](time-management.md)
- [WiFi](wifi-management.md)
- [Windows Update Management](windows-update-management.md)
- [Report All Device Properties](report-all-device-properties.md)
