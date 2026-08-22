# Access FM220U L1 enrollment

The registration page now captures eight fingers in this order:

1. Right thumb
2. Right index
3. Right middle
4. Right ring
5. Left thumb
6. Left index
7. Left middle
8. Left ring

The browser cannot access the FM220U USB device directly. Install the Access/RD Service or vendor SDK on the registration computer and expose a local device bridge. Configure the frontend with:

```text
NEXT_PUBLIC_BIOMETRIC_BRIDGE_URL=http://127.0.0.1:11100
```

The Access FM220U L1 RD Service must accept the standard capture request:

```http
CAPTURE /
Content-Type: text/xml
```

For a finger enrollment capture:

```json
<PidOptions ver="1.0"><Opts fCount="1" fType="2" iCount="0" iType="0" pCount="0" pType="0" format="0" pidVer="2.0" timeout="10000" otp="" wadh="" posh="UNKNOWN" env="P" /></PidOptions>
```

The response must be JSON containing the provider template without storing it in the browser:

```json
{
  "templateDataBase64": "<base64 template returned by the approved device SDK>"
}
```

The frontend sends each template immediately to the EWMS API and does not persist raw fingerprint data. Registration completion is blocked unless all eight fingers are enrolled and the final verification succeeds.

After all eight fingers are enrolled, registration completes by finalizing the provider enrollment. No ninth fingerprint scan or separate registration verification scan is required.

The current attendance API is authenticated and accepts `EmployeeId` for check-in. Aadhaar is collected during registration and should be used as an identity lookup only through a protected attendance workflow; do not expose a public endpoint that accepts Aadhaar alone without biometric verification and rate limiting.
