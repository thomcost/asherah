package com.godaddy.asherah.testapp.utils;

import static com.godaddy.asherah.testapp.testhelpers.Constants.KEY_MANAGEMENT_AWS;
import static com.godaddy.asherah.testapp.testhelpers.Constants.KEY_MANAGEMENT_STATIC_MASTER_KEY;

import com.godaddy.asherah.appencryption.keymanagement.AWSKeyManagementServiceImpl;
import com.godaddy.asherah.appencryption.keymanagement.KeyManagementService;
import com.godaddy.asherah.appencryption.keymanagement.StaticKeyManagementServiceImpl;
import com.godaddy.asherah.testapp.configuration.ServerConfiguration;

public final class KeyManagementServiceFactory {

  private KeyManagementServiceFactory() {

  }

  public static KeyManagementService createKeyManagementService(final ServerConfiguration configuration, final String kmsType) {
    if (kmsType.equalsIgnoreCase(KEY_MANAGEMENT_AWS)) {
      return AWSKeyManagementServiceImpl
          .newBuilder(configuration.getKmsAwsRegionMap(), configuration.getKmsAwsPreferredRegion())
          .build();
    }
    else {
      return new StaticKeyManagementServiceImpl(KEY_MANAGEMENT_STATIC_MASTER_KEY);
    }
  }
}
