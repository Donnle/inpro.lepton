import * as jspb from 'google-protobuf'

import * as google_protobuf_empty_pb from 'google-protobuf/google/protobuf/empty_pb'; // proto import: "google/protobuf/empty.proto"


export class LoginRequest extends jspb.Message {
  getUsernameoremail(): string;
  setUsernameoremail(value: string): LoginRequest;

  getPassword(): string;
  setPassword(value: string): LoginRequest;

  getTenancyname(): string;
  setTenancyname(value: string): LoginRequest;

  getRememberme(): boolean;
  setRememberme(value: boolean): LoginRequest;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): LoginRequest.AsObject;
  static toObject(includeInstance: boolean, msg: LoginRequest): LoginRequest.AsObject;
  static serializeBinaryToWriter(message: LoginRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): LoginRequest;
  static deserializeBinaryFromReader(message: LoginRequest, reader: jspb.BinaryReader): LoginRequest;
}

export namespace LoginRequest {
  export type AsObject = {
    usernameoremail: string;
    password: string;
    tenancyname: string;
    rememberme: boolean;
  };
}

export class LoginReply extends jspb.Message {
  getSuccess(): boolean;
  setSuccess(value: boolean): LoginReply;

  getRequirestwofactor(): boolean;
  setRequirestwofactor(value: boolean): LoginReply;

  getIslockedout(): boolean;
  setIslockedout(value: boolean): LoginReply;

  getError(): string;
  setError(value: string): LoginReply;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): LoginReply.AsObject;
  static toObject(includeInstance: boolean, msg: LoginReply): LoginReply.AsObject;
  static serializeBinaryToWriter(message: LoginReply, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): LoginReply;
  static deserializeBinaryFromReader(message: LoginReply, reader: jspb.BinaryReader): LoginReply;
}

export namespace LoginReply {
  export type AsObject = {
    success: boolean;
    requirestwofactor: boolean;
    islockedout: boolean;
    error: string;
  };
}

export class CheckPasswordRequest extends jspb.Message {
  getUsernameoremail(): string;
  setUsernameoremail(value: string): CheckPasswordRequest;

  getPassword(): string;
  setPassword(value: string): CheckPasswordRequest;

  getTenancyname(): string;
  setTenancyname(value: string): CheckPasswordRequest;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): CheckPasswordRequest.AsObject;
  static toObject(includeInstance: boolean, msg: CheckPasswordRequest): CheckPasswordRequest.AsObject;
  static serializeBinaryToWriter(message: CheckPasswordRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): CheckPasswordRequest;
  static deserializeBinaryFromReader(message: CheckPasswordRequest, reader: jspb.BinaryReader): CheckPasswordRequest;
}

export namespace CheckPasswordRequest {
  export type AsObject = {
    usernameoremail: string;
    password: string;
    tenancyname: string;
  };
}

export class CheckPasswordReply extends jspb.Message {
  getIsvalid(): boolean;
  setIsvalid(value: boolean): CheckPasswordReply;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): CheckPasswordReply.AsObject;
  static toObject(includeInstance: boolean, msg: CheckPasswordReply): CheckPasswordReply.AsObject;
  static serializeBinaryToWriter(message: CheckPasswordReply, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): CheckPasswordReply;
  static deserializeBinaryFromReader(message: CheckPasswordReply, reader: jspb.BinaryReader): CheckPasswordReply;
}

export namespace CheckPasswordReply {
  export type AsObject = {
    isvalid: boolean;
  };
}

