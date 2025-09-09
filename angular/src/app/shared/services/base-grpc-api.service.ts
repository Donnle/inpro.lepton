import { Injectable } from '@angular/core';
import { LoginServiceClient } from '../../grpc/LoginServiceClientPb';
import { CheckPasswordRequest } from '../../grpc/login_pb';

@Injectable({ providedIn: 'root' })
export class BaseGrpcApiService {
  readonly loginServiceClient: LoginServiceClient = new LoginServiceClient('https://localhost:5001', null, null);

  test() {
    const req = new CheckPasswordRequest();

    req
      .setUsernameoremail('user.test@gmail.com')
      .setPassword('userTest100!');

    this.loginServiceClient.checkPassword(req, {}, (err, resp) => {
      if (err) {
        console.error('gRPC error', {
          code: err.code,
          message: err.message,
          metadata: err.metadata?.headersMap
        });

        return;
      }

      console.log(resp?.toObject());
    });
  }
}
