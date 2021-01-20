# azure-csi-driver-sample

Sample application for testing Azure Key Vault integration in Kubernetes using the CSI Driver.

![Publish Docker image workflow](https://github.com/fldsblzs/azure-csi-driver-sample/workflows/Publish%20Docker%20image/badge.svg)

## About

This project was created to demonstrate a simple scenario for integrating Azure [Key Vault](https://azure.microsoft.com/en-us/services/key-vault/) into Kubernetes. In this case the application secrets are stored in the Key Vault and being mounted into the actual pods during deployment. Automatic secret rotation after the secret was updated in the Key Vault is also handled using [Reloader](https://github.com/stakater/Reloader).

## References

- The official documentation about [integrating Azure Key Vault into Kubernetes](https://docs.microsoft.com/en-us/azure/key-vault/general/key-vault-integrate-kubernetes).
- The [Reloader](https://github.com/stakater/Reloader) repository.
- The [Azure Key Vault Provider for Secrets Store CSI Driver](https://github.com/Azure/secrets-store-csi-driver-provider-azure) repository.

## Project structure

- `.github\workflows` folder contains the GitHub action's definition that builds and pushes the sample app's image to [DockerHub](https://hub.docker.com/r/fblzs/azure-csi-driver-sample).
- `deployments` folder contains the namespace definition and the helm chart with templates to deploy the sample application with a basic setup.
- `src\AzureCsiDriver.Application` folder contains the sample application.

## The scenario

TODO

### Prerequisites

TODO

### The sample application

TODO

### Deployment files

TODO

### Testing

TODO
