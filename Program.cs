using System.Threading.Tasks;
using System.Collections.Generic;
using Pulumi;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.Ec2.Inputs;
using NewRelic = Pulumi.NewRelic;

class Program
{
    static Task<int> Main()
    {
        const int amountEC2Instances = 10;

        return Deployment.RunAsync(() =>
        {
            var ami = GetAmi.Invoke(new GetAmiInvokeArgs
            {
                Owners = { "137112412989" }, // This owner ID is Amazon
                MostRecent = true,
                Filters =
                {
                    new GetAmiFilterInputArgs
                    {
                        Name = "name",
                        Values =  { "amzn2-ami-hvm-*" },
                    },
                },
            });

            var group = new SecurityGroup("webserver-secgrp", new SecurityGroupArgs
            {
                Ingress = new[]
                {
                    new SecurityGroupIngressArgs
                    {
                        Protocol = "tcp",
                        FromPort = 22,
                        ToPort = 22,
                        CidrBlocks = { "0.0.0.0/0" }
                    },
                    new SecurityGroupIngressArgs
                    {
                        Protocol = "tcp",
                        FromPort = 80,
                        ToPort = 80,
                        CidrBlocks = { "0.0.0.0/0" }
                    },
                },
                Egress = new[]
                {
                    new SecurityGroupEgressArgs
                    {
                        Protocol = "tcp",
                        // Allow all outbound traffic‚
                        FromPort = 80,
                        ToPort = 80,
                        CidrBlocks = { "0.0.0.0/0" }
                    },
                }
            });

            var userData = @"
                #!/bin/bash
                echo ""Hello, World!"" > index.html
                nohup python -m SimpleHTTPServer 80 &
            ";

            List<object?> instanceIds = new List<object?>();
            for (int i = 0; i < amountEC2Instances; i++)
            {
                string instanceName = $"webserver-www-{i}";
                var server = new Instance(instanceName, new InstanceArgs
                {
                    // t2.micro is available in the AWS free tier
                    InstanceType = "t2.micro",
                    VpcSecurityGroupIds = { group.Id }, // reference the security group resource above
                    UserData = userData,
                    Ami = ami.Apply(x => x.Id),
                    Tags = { { "Name", instanceName } }
                });

                instanceIds.Add(server.Id.Apply(t => { return $"{t}" as string; }));

                // create New Relic Synthetics Monitor based on the public IP of the EC2 instance
                var monitor = new Pulumi.NewRelic.Synthetics.Monitor(instanceName, new()
                {
                    Status = "ENABLED",
                    Name = instanceName,
                    Period = "EVERY_MINUTE",
                    Uri = server.PublicIp.Apply(t => $"http://{t}"),
                    Type = "BROWSER",
                    LocationsPublics = new[]
                    {
                        "AWS_US_WEST_2",
                    },
                    CustomHeaders = new[]
                    {
                        new NewRelic.Synthetics.Inputs.MonitorCustomHeaderArgs
                        {
                            Name = "some_name",
                            Value = "some_value",
                        },
                    },
                    TreatRedirectAsFailure = true,
                    ValidationString = "Hello, World!",
                    BypassHeadRequest = true,
                    VerifySsl = false,
                    RuntimeTypeVersion = "100",
                    RuntimeType = "CHROME_BROWSER",
                    Tags = new[]
                    {
                        new NewRelic.Synthetics.Inputs.MonitorTagArgs
                        {
                            Key = "O11y-as-Code",
                            Values = new[]
                            {
                                "Pulumi",
                            },
                        },
                    },
                });
            }

            return new Dictionary<string, object?>
            {
                ["instanceIds"] = instanceIds.ToArray()
            };
        });
    }
}