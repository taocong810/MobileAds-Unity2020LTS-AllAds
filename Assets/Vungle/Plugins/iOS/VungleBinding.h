//
//  VungleBinding.h
//  Vungle Unity Plugin 6.9.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//
//  This is the bridge between Unity and the native iOS SDK
//  This header file isn't necessary unless the plugin code is compiled into
//  a static library
//

#import <Foundation/Foundation.h>

#ifndef VungleBinding_h
#define VungleBinding_h

extern "C" {
#pragma mark - Debugging
    char * _vungleGetSdkVersion();
    void _vungleEnableLogging(BOOL shouldEnable);

#pragma mark - Initialization
    void _vungleStartWithAppId(const char * appId, const char * pluginVersion, BOOL initHeaderBiddingDelegate);
    BOOL _vungleIsInitialized();
    void _vungleSetPublishIDFV(BOOL shouldEnable);
    void _vungleSetMinimumSpaceForInit(int minimumSize);
    void _vungleSetMinimumSpaceForAd(int minimumSize);
    void _vungleSetSoundEnabled(BOOL enabled);
    BOOL _vungleIsSoundEnabled();

#pragma mark - Consent
    void _updateConsentStatus(int status, const char * version);
    int _getConsentStatus();
    void _updateCCPAStatus(int status);
    int _getCCPAStatus();

#pragma mark - AdLifecycle-Fullscreen
    BOOL _vungleIsAdAvailable(const char* placementID);
    BOOL _vungleLoadAd(const char* placementID);
    void _vunglePlayAd(char* opt, const char* placementID);
    // Not used anymore since flex ads are gone... Remove?
    BOOL _vungleCloseAd(const char* placementID);

#pragma mark - AdLifecycle-Banner
    BOOL _vungleIsBannerAvailable(const char* placementID, int size);
    void _vungleLoadBanner(const char* placementID, int bannerSize, int bannerPosition);
    void _vungleSetOffset(const char* placementID, int x, int y);
    void _vungleShowBanner(const char* placementID, const char* options);
    void _vungleCloseBanner(const char* placementID);

#pragma mark - Unity helper
    void _requestTrackingAuthorization();
}

#endif
