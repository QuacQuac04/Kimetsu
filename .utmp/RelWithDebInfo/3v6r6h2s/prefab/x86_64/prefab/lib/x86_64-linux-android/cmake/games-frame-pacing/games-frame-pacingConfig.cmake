if(NOT TARGET games-frame-pacing::swappy)
add_library(games-frame-pacing::swappy SHARED IMPORTED)
set_target_properties(games-frame-pacing::swappy PROPERTIES
    IMPORTED_LOCATION "/Users/quacquac/.gradle/caches/8.13/transforms/6553fed145da0193876e5b502bd35441/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy/libs/android.x86_64/libswappy.so"
    INTERFACE_INCLUDE_DIRECTORIES "/Users/quacquac/.gradle/caches/8.13/transforms/6553fed145da0193876e5b502bd35441/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy/include"
    INTERFACE_LINK_LIBRARIES ""
)
endif()

if(NOT TARGET games-frame-pacing::swappy_static)
add_library(games-frame-pacing::swappy_static STATIC IMPORTED)
set_target_properties(games-frame-pacing::swappy_static PROPERTIES
    IMPORTED_LOCATION "/Users/quacquac/.gradle/caches/8.13/transforms/6553fed145da0193876e5b502bd35441/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy_static/libs/android.x86_64/libswappy_static.a"
    INTERFACE_INCLUDE_DIRECTORIES "/Users/quacquac/.gradle/caches/8.13/transforms/6553fed145da0193876e5b502bd35441/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy_static/include"
    INTERFACE_LINK_LIBRARIES ""
)
endif()

