import keras
import numpy as np

(x_train, _), (x_test, _) = keras.datasets.mnist.load_data()
x_train = x_train.astype('float32') / 255.
x_test = x_test.astype('float32') / 255.
x_train = np.reshape(x_train, (len(x_train), 28, 28, 1))
x_test = np.reshape(x_test, (len(x_test), 28, 28, 1))


sigma = 0.8
x_train_noisy = x_train + np.random.normal(loc=0.0, scale=sigma, size=x_train.shape)
x_test_noisy = x_test + np.random.normal(loc=0.0, scale=sigma, size=x_test.shape)


def create_model():
    input_image = keras.layers.Input(shape=(28, 28, 1))
    x = keras.layers.Conv2D(32, (3, 3), activation='relu', padding='same')(input_image)
    x = keras.layers.MaxPooling2D((2, 2), padding='same')(x)
    encoded = keras.layers.MaxPooling2D((2, 2), padding='same')(x)
    x = keras.layers.Conv2D(32, (3, 3), activation='relu', padding='same')(encoded)
    x = keras.layers.UpSampling2D((2, 2))(x)
    x = keras.layers.Conv2D(32, (3, 3), activation='relu', padding='same')(x)
    x = keras.layers.UpSampling2D((2, 2))(x)
    decoded = keras.layers.Conv2D(1, (3, 3), activation='sigmoid', padding='same')(x)
    model = keras.models.Model(input_image, decoded)
    model.compile(optimizer='adadelta', loss='binary_crossentropy')
    model.fit(x_train_noisy, x_train, epochs=101, batch_size=128, shuffle=True, validation_data=(x_test_noisy, x_test))
    model.save('filter_model.keras')
    return model
def load_model():
    return keras.models.load_model('filter_model.keras')

import os
if os.path.exists('filter_model.keras'):
    model = load_model()
else:
    model = create_model()

keras.utils.plot_model(model, "model.png", show_shapes=True)

import matplotlib.pyplot as plt

reconstructed_images = model.predict(x_test_noisy)

n = 4


indexes = [8, 8+500, 8+1000, 8+1500]

for i in range(n):
    # original
    ax = plt.subplot(3, 4, i+1)
    plt.imshow(x_test[indexes[i]].reshape(28, 28))
    plt.gray()
    ax.get_xaxis().set_visible(False)
    ax.get_yaxis().set_visible(False)

    # noisy
    ax = plt.subplot(3, 4, i + 4+1)
    plt.imshow(x_test_noisy[indexes[i]].reshape(28, 28))
    plt.gray()
    ax.get_xaxis().set_visible(False)
    ax.get_yaxis().set_visible(False)

    # reconstructed
    ax = plt.subplot(3, 4, i + 8+1)
    plt.imshow(reconstructed_images[indexes[i]].reshape(28, 28))
    plt.gray()
    ax.get_xaxis().set_visible(False)
    ax.get_yaxis().set_visible(False)

plt.savefig('result.png')