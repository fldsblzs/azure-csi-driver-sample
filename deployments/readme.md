# Deployment files

Additional information about each deployment file and the helm install process.

## Helm install

In this example scenario the CSI Driver and Reloader are deployed into the `configuration` namespace. 

The Kubernetes secret containing the Azure service principal credentials needs to be created in the application's namespace (`azure-csi-driver-sample-test`):

```
kubectl -n azure-csi-driver-sample-test create secret generic secrets-store-creds --from-literal=clientid=<service principal id> --from-literal=clientsecret=<service principal secret>
```

The sample `azure-csi-driver-sample-test` namespace can be created via `kubectl`:

```
kubectl apply -f deployments\namespace.yaml
```

Before installing the chart configure the secret provider with valid values for your Azure Key Vault. Please note if you change anything else it might require other changes in `values.yaml` file to make sure everything references the right names.

```yaml
secretProvider:
  name: 'application-secret-provider'
  secretName: 'application-secrets'
  # Azure Key Vault name
  keyVaultname: ''
  # Azure Resource Group where the Key Vault was created
  resourceGroup: ''
  # Id of the Azure subscription where the Resource Group was created
  subscriptionId: ''
  # Id of the tenant where the Azure subscription was created 
  tenantId: ''
```

For installing the chart simply use:

```
helm install azure-csi-driver-sample .
```

Additionally, the namespace for the application components can also be specified in the `values.yaml` file:

```yaml
global:
  namespace: 'azure-csi-driver-sample-test'
```

## Application

The application is set up using a simple `Deployment` kind. The specified volume is called `secrets-store-inline` and the related volumeMount mounts the data to the path `/mnt/secrets-store` which will be read during the applications startup configuration via KeyPerFile provider.

```yaml
---
        volumeMounts:
          - name: secrets-store-inline
            mountPath: "/mnt/secrets-store"
            readOnly: true
      volumes:
        - name: secrets-store-inline
          csi:
            driver: secrets-store.csi.k8s.io
            readOnly: true
            volumeAttributes:
              secretProviderClass: {{ $.Values.app.secretProviderClassName }}
            nodePublishSecretRef:
              name: secrets-store-creds 
```

`secretProviderClass` should reference the exact name of resource defined in `secret-provider.yaml`. `nodePublishSecretRef` references the secret by name that was created in the **same** namespace with the service principal credentials.

Reloder was configured to watch for the secret created by the `SecretProviderClass`:

```
  annotations:
    secret.reloader.stakater.com/reload: {{ $.Values.app.reloaderSecretName }}
```

## Service

The service is a minimal set up for a load balancer type (*nothing to see here*).

## Secret Provider

The `SecretProviderClass` has some required parameters to specify the Azure resource to use (keyvaultName, resourceGroup, subscriptionId, tenantId).

More importantly this is the place where the actual secret we want to use from the Key Vault can be specified. `objectName` is the exact name defined for the secret in the Key Vault, `objectAlias` is there to ease the configuration process for the ASP.NET Core application. `objectType` is secret as we use a secret resource from the Key Vault.

```yaml
  parameters:
    keyvaultName: {{ $.Values.secretProvider.keyVaultname }}
    objects:  |
      array:
        - |
          objectName: TestApp-Notification-MessagingPassword
          objectAlias: Notification:MessagingPassword
          objectType: secret
    resourceGroup: {{ $.Values.secretProvider.resourceGroup }}
    subscriptionId: {{ $.Values.secretProvider.subscriptionId }}
    tenantId: {{ $.Values.secretProvider.tenantId }}
```

An additional Kubernetes secret resource is managed by the `SecretProviderClass` which is used to rotate secret values once they are changed in the Key Vault (via Reloader):

**Important**: if an `objectAlias` is used in the secrets array it should be used here for the `objectName` - otherwise it won't work (at least it didn't the last time I tried).

```yaml
  secretObjects:
  - secretName: {{ $.Values.secretProvider.secretName }}
    type: Opaque
    data:
    - key: Notification__MessagingPassword
      objectName: Notification:MessagingPassword
```