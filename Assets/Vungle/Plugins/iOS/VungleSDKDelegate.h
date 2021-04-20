//
//  VungleSDKDelegate.h
//  Vungle Unity Plugin 6.9.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//
//  This class handles all delegate callbacks from the SDK
//  and relays the callback into readable messages to Unity
//
//  Logging messages should be placed into the Utility class
//  so other classes can use the static method to pass messages
//  to Unity
//

#import <VungleSDK/VungleSDK.h>
#import <VungleSDK/VungleSDKHeaderBidding.h>
#import <VungleSDK/VungleSDKCreativeTracking.h>

@interface VungleSDKDelegate : NSObject <VungleSDKDelegate, VungleSDKHeaderBidding, VungleSDKCreativeTracking, VungleSDKLogger>
{

}
+ (VungleSDKDelegate*)instance;

@end

#ifndef VungleSDKDelegate_h
#define VungleSDKDelegate_h


#endif /* VungleSDKDelegate_h */
