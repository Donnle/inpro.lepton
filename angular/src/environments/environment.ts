import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

export const environment = {
  production: false,
  application: {
    baseUrl,
    name: 'lepton',
    logoUrl: '',
  },
  oAuthConfig: {
    issuer: 'http://localhost:5000/',
    redirectUri: baseUrl,
    clientId: 'lepton_App',
    responseType: 'code',
    scope: 'offline_access lepton',
    requireHttps: true,
  },
  apis: {
    default: {
      url: 'http://localhost:5000',
      rootNamespace: 'inpro.lepton',
    },
  },
} as Environment;
