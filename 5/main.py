import cv2
import numpy as np
import math
import os
import random
import matplotlib.pyplot as plt


def segmentImage(anchor, blockSize=16):
    h, w = anchor.shape
    hSegments = int(h / blockSize)
    wSegments = int(w / blockSize)
    return hSegments, wSegments


def getCenter(x, y, blockSize):
    return int(x + blockSize / 2), int(y + blockSize / 2)


def getAnchorSearchArea(x, y, anchor, blockSize, searchArea):
    h, w = anchor.shape
    cx, cy = getCenter(x, y, blockSize)
    sx = max(0, cx - int(blockSize / 2) - searchArea)
    sy = max(0, cy - int(blockSize / 2) - searchArea)
    return anchor[
        sy : min(sy + searchArea * 2 + blockSize, h),
        sx : min(sx + searchArea * 2 + blockSize, w),
    ]


def getBlockZone(p, aSearch, tBlock, blockSize):
    px, py = p
    px, py = px - int(blockSize / 2), py - int(blockSize / 2)
    px, py = max(0, px), max(0, py)
    aBlock = aSearch[py : py + blockSize, px : px + blockSize]
    assert aBlock.shape == tBlock.shape
    return aBlock


def getMAD(tBlock, aBlock):
    return np.sum(np.abs(np.subtract(tBlock, aBlock))) / (
        tBlock.shape[0] * tBlock.shape[1]
    )


def getBestMatch(tBlock, aSearch, blockSize):
    step = 4
    ah, aw = aSearch.shape
    acy, acx = int(ah / 2), int(aw / 2)
    minMAD = float("+inf")
    minP = None
    while step >= 1:
        pointList = [
            (acx, acy),
            (acx + step, acy),
            (acx, acy + step),
            (acx + step, acy + step),
            (acx - step, acy),
            (acx, acy - step),
            (acx - step, acy - step),
            (acx + step, acy - step),
            (acx - step, acy + step),
        ]

        for p in pointList:
            aBlock = getBlockZone(p, aSearch, tBlock, blockSize)
            MAD = getMAD(tBlock, aBlock)
            if MAD < minMAD:
                minMAD = MAD
                minP = p
        step //= 2
    px, py = minP
    px, py = px - int(blockSize / 2), py - int(blockSize / 2)
    px, py = max(0, px), max(0, py)
    return aSearch[py : py + blockSize, px : px + blockSize]


def blockSearchBody(anchor, target, blockSize, searchArea=7):
    h, w = anchor.shape
    hSegments, wSegments = segmentImage(anchor, blockSize)
    predicted = np.ones((h, w)) * 255
    bcount = 0
    for y in range(0, int(hSegments * blockSize), blockSize):
        for x in range(0, int(wSegments * blockSize), blockSize):
            bcount += 1
            targetBlock = target[y : y + blockSize, x : x + blockSize]
            anchorSearchArea = getAnchorSearchArea(x, y, anchor, blockSize, searchArea)
            anchorBlock = getBestMatch(targetBlock, anchorSearchArea, blockSize)
            predicted[y : y + blockSize, x : x + blockSize] = anchorBlock
    assert bcount == int(hSegments * wSegments)
    return predicted


def getResidual(target, predicted):
    return np.subtract(target, predicted)


def getReconstructTarget(residual, predicted):
    return np.add(residual, predicted)


def getBitsPerPixel(im):
    h, w = im.shape
    bits = 0
    for row in im.tolist():
        for pixel in row:
            bits += math.log2(abs(pixel) + 1)
    return bits / (h * w)


def getFrames(filename, first_frame, second_frame):
    print(f"Getting frames {first_frame} and {second_frame} from {filename}")
    cap = cv2.VideoCapture(filename)
    cap.set(cv2.CAP_PROP_POS_FRAMES, first_frame - 1)
    res1, fr1 = cap.read()
    cap.set(cv2.CAP_PROP_POS_FRAMES, second_frame - 1)
    res2, fr2 = cap.read()
    cap.release()
    return fr1, fr2


def main():
    filename = "video.mp4"
    outfile = "output_frames"
    saveOutput = True

    fr = random.randint(0, 1000)
    anchorFrame, targetFrame = getFrames(filename, fr, fr + 1)

    bitsAnchor = []
    bitsDiff = []
    bitsPredicted = []
    h, w, ch = anchorFrame.shape

    diffFrameRGB = np.zeros((h, w, ch))
    predictedFrameRGB = np.zeros((h, w, ch))
    residualFrameRGB = np.zeros((h, w, ch))
    restoreFrameRGB = np.zeros((h, w, ch))

    for i in range(3):
        anchorFrame_c = anchorFrame[:, :, i]
        targetFrame_c = targetFrame[:, :, i]

        diffFrame = cv2.absdiff(anchorFrame_c, targetFrame_c)
        predictedFrame = blockSearchBody(anchorFrame_c, targetFrame_c, 16)
        residualFrame = getResidual(targetFrame_c, predictedFrame)
        reconstructTargetFrame = getReconstructTarget(residualFrame, predictedFrame)

        bitsAnchor.append(getBitsPerPixel(anchorFrame_c))
        bitsDiff.append(getBitsPerPixel(diffFrame))
        bitsPredicted.append(getBitsPerPixel(residualFrame))

        diffFrameRGB[:, :, i] = diffFrame
        predictedFrameRGB[:, :, i] = predictedFrame
        residualFrameRGB[:, :, i] = residualFrame
        restoreFrameRGB[:, :, i] = reconstructTargetFrame

    if saveOutput:
        os.makedirs(outfile, exist_ok=True)
        cv2.imwrite(f"{outfile}/First_frame.png", anchorFrame)
        cv2.imwrite(f"{outfile}/Second_frame.png", targetFrame)
        cv2.imwrite(f"{outfile}/Difference_frame.png", diffFrameRGB)
        cv2.imwrite(f"{outfile}/Predicted_frame.png", predictedFrameRGB)
        cv2.imwrite(f"{outfile}/Residual_frame.png", residualFrameRGB)
        cv2.imwrite(f"{outfile}/Restore_frame.png", restoreFrameRGB)

    barWidth = 0.25
    P1 = [sum(bitsAnchor), *bitsAnchor]
    Diff = [sum(bitsDiff), *bitsDiff]
    Mpeg = [sum(bitsPredicted), *bitsPredicted]

    fig = plt.subplots(figsize=(12, 8))
    br1 = np.arange(len(P1))
    br2 = [x + barWidth for x in br1]
    br3 = [x + barWidth for x in br2]

    plt.bar(
        br1, P1, color="r", width=barWidth, edgecolor="grey", label="Initial Frame Size"
    )
    plt.bar(
        br2,
        Diff,
        color="g",
        width=barWidth,
        edgecolor="grey",
        label="Frame Difference Size",
    )
    plt.bar(
        br3,
        Mpeg,
        color="b",
        width=barWidth,
        edgecolor="grey",
        label="Motion Compensated Frame Size",
    )
    plt.title(f"Compression Ratio = {round(sum(bitsAnchor) / sum(bitsPredicted), 2)}")
    plt.ylabel("Bits per Pixel")
    plt.xticks([r + barWidth for r in range(len(P1))], ["RGB", "R", "G", "B"])
    plt.legend()
    plt.savefig(f"{outfile}/Compression_Histogram.png", dpi=600)


if __name__ == "__main__":
    main()
