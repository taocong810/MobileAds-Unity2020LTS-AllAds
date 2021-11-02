//
//  VungleUtility.h
//  Vungle Unity Plugin 6.9.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//

@interface VungleUtility : NSObject

+ (NSString * _Nonnull)jsonFromObject:(id _Nonnull )object;
+ (id _Nonnull )objectFromJson:(NSString * _Nonnull)json;

// Handling issues with legacy / MREC ads and background audio
+ (void)pauseAudioSession;
+ (void)revertAudioSession;

// Callbacks to Unity used by multiple places
+ (void)setLoggingEnabled:(BOOL)isEnabled;
+ (void)sendLog:(NSString * _Nonnull)message;
+ (void)sendWarningMessage:(NSString * _Nonnull)message;
+ (void)sendErrorMessage:(NSString * _Nonnull)message;

@end

#ifndef VungleUtility_h
#define VungleUtility_h


#endif /* VungleUtility_h */
