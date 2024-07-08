# Pulumi AWS sample with C# .NET

This sample repo shows a sample implementation of Pulumi in order to create AWS EC2 instances. It includes the selection of the latest Amazon AMI and creates a sample security group with SSH (port 22) and HTTP (port 80) ingress and HTTP egress (port 80).

As part of the creation of the EC2 instance, we add a user data script that basically only spins up a simple HTTP web server including a hello world message in the index.html.
 In a second step, it uses the public IP address of that EC2 instance to create a New Relic Synthetics monitor to constantly trigger a simble browser check against it's HTTP endpoint.

## Prerequisites

Your AWS CLI has to be properly configured to access your AWS account. Your the following command to achieve this:

```shell
aws configure
```

For the creation of the New Relic Synthetics entities, you have to provide your New Relic user API key and account ID in the [Pulumi.aws.yaml](/Pulumi.aws.yaml) file.
