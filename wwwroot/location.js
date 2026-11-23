window.municipalityLocation = {
    getCurrentPosition: function () {
        return new Promise(function (resolve, reject) {
            if (!navigator.geolocation) {
                reject(new Error("המכשיר אינו תומך באיתור מיקום."));
                return;
            }

            navigator.geolocation.getCurrentPosition(
                function (position) {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude
                    });
                },
                function (error) {
                    reject(new Error(error.message));
                },
                { enableHighAccuracy: true, timeout: 10000 }
            );
        });
    }
};
