# azure-csi-driver-sample

Sample application for testing Azure Key Vault integration in Kubernetes using the CSI Driver.

![Publish Docker image workflow](https://github.com/fldsblzs/azure-csi-driver-sample/workflows/Publish%20Docker%20image/badge.svg)

## About

This project was created to demonstrate a simple scenario for integrating [Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault/) into Kubernetes ([Azure Kubernetes Service](https://azure.microsoft.com/en-us/services/kubernetes-service/)). In this case the application secrets are stored in the Key Vault and being mounted into the actual pods during deployment. Automatic secret rotation after the secret was updated in the Key Vault is also handled using [Reloader](https://github.com/stakater/Reloader).

## Project structure

- `.github\workflows` folder contains the GitHub action's definition that builds and pushes the sample app's image to [DockerHub](https://hub.docker.com/r/fblzs/azure-csi-driver-sample).
- `deployments` folder contains the namespace definition and the helm chart with templates to deploy the sample application with a basic setup.
- `src\AzureCsiDriver.Application` folder contains the sample application.

## The scenario

As a starting point [Azure Key Vault Provider for Secrets Store CSI Driver](https://github.com/Azure/secrets-store-csi-driver-provider-azure) and [Reloader](https://github.com/stakater/Reloader) are deployed into the cluster in separate namespace called `configuration`. This namespace was created to have a single namespace containing all resources that handle some kind of configuration functionality and can be used by all kind of applications in the cluster. Both the CSI Driver and Reloader support cross-namespace usage out-of-the-box.

Then a namespace called `azure-csi-driver-sample-test` was created for the sample application. The application has a basic setup with a deployment, a service and a `SecretProviderClass` resource. The related `SecretProviderClass` was configured to mount and sync the right secret from the Key Vault.

**Important**: The service principal created for the CSI Driver to access the Key Vault is created in the namespace `SecretProviderClass` as a Kubernetes secret resource. The secret can be created via `kubectl`:

```
kubectl -n azure-csi-driver-sample-test create secret generic secrets-store-creds --from-literal=clientid=<service principal id> --from-literal=clientsecret=<service principal secret>
```

### Prerequisites

- A Kubernetes cluster

- The official docs on [integrating Azure Key Vault into Kubernetes](https://docs.microsoft.com/en-us/azure/key-vault/general/key-vault-integrate-kubernetes) covers all required steps to set up Azure Key Vault Provider for Secrets Store CSI Driver. This includes creating the Key Vault, installing the CSI Driver and creating the managed identity. The `SecretProviderClass` and sample application deployment can be skipped as it does not care about namespaces.

```
helm repo add csi-secrets-store-provider-azure https://raw.githubusercontent.com/Azure/secrets-store-csi-driver-provider-azure/master/charts

helm install --set secrets-store-csi-driver.enableSecretRotation=true,secrets-store-csi-driver.rotationPollInterval=5m csi-secrets-store-provider-azure/csi-secrets-store-provider-azure --generate-name --namespace configuration
```

- [Reloader](https://github.com/stakater/Reloader) should be installed in `configuration` namespace.

```
helm install stakater/reloader --namespace configuration
```

### The sample application

The sample application is a lightweight ASP.NET Core 5 Web API with a single GET endpoint (`/Configuration`) for reading its current configuration. The imaginary configuration for a messaging abstraction is read into the `NotificationOptions` class. 

```C#
    public bool ShouldSendErrorNotifications { get; set; }
    public string MessagingQueue { get; set; }
    public string MessagingUserName { get; set; }
    public string MessagingPassword { get; set; }
```

Except the `MessagingPassword` all of these are coming from the `appsettings.json` file:

```json
  "Notification": {
    "ShouldSendErrorNotifications": true,
    "MessagingQueue": "sample-queue",
    "MessagingUserName": "sample-user"
  }
```

The `MessagingPassword` comes from an optional file - this will be mounted using the CSI Driver:

```C#
builder.AddKeyPerFile("/mnt/secrets-store", true);
```

Building the docker image locally:

```
cd src\AzureCsiDriver.Application
docker build --no-cache --rm  -t local/azure-csi-driver-sample:v1 .
```

Running the application locally in docker:

```
docker run --rm -p 8000:80 local/azure-csi-driver-sample:v1
```

Or simply using the pre-built image from DockerHub:

```
docker run --rm -p 8000:80 fblzs/azure-csi-driver-sample:v1
```

### Deployment files

The deployment was set up using a helm chart. It contains the deployment manifests for the application (`application.yaml`), its load balancer (`service.yaml`) and the related SecretProviderClass custom resource definition (`secret-provider.yaml`).

**Important**: `values.yaml` should be configured accordingly.

```
helm install azure-csi-driver-sample .
```

More information about each template file can be found in `deployments\readme.md` file.

### Testing the functionality

Running locally, the GET `http://localhost:8000/Configuration` request return the following payload:

```json
{
  "shouldSendErrorNotifications": true,
  "messagingQueue": "sample-queue",
  "messagingUserName": "sample-user",
  "messagingPassword": null
}
```

`messagingPassword` is null since secret files are not mounted yet.

After deploying to Kubernetes the secrets specified in `SecretProviderClass` will be mounted under `/mnt/secrets-store` and the results should change accordingly:

```json
{
  "shouldSendErrorNotifications": true,
  "messagingQueue": "sample-queue",
  "messagingUserName": "sample-user",
  "messagingPassword": "sensitive-secret-from-the-kv"
}
```

### Secret Rotation

This feature is not enabled by default so make sure the CSI Driver is installed setting the following values:

```
--set secrets-store-csi-driver.enableSecretRotation=true,secrets-store-csi-driver.rotationPollInterval=5m
```

- `enableSecretRotation`: enables secret rotation feature
- `rotationPollInterval`: polling interval

The secret from the Key Vault is also synced into a Kubernetes secret resource. Reloader can be configured to watch for the specific secret to change:

```yaml
annotations:
    secret.reloader.stakater.com/reload: {{ $.Values.app.reloaderSecretName }}
```

Once the secret is changed in the Key Vault the `SecretProviderClass` will update the corresponding Kubernetes secret. This change will be caught by Reloader which results in a restarted pod with the updated secret value mounted under `/mnt/secrets-store`.