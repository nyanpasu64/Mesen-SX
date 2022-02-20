#pragma once 
#include "stdafx.h"
#include <mutex>
#include <thread>

class SimpleLock;

class LockHandler
{
private:
	SimpleLock *_lock;
public:
	LockHandler(SimpleLock *lock);
	~LockHandler();
};

class SimpleLock
{
private:
	std::recursive_mutex _mutex;

public:
	SimpleLock();
	~SimpleLock();

	LockHandler AcquireSafe();

	void Acquire();
	void WaitForRelease();
	void Release();
};

